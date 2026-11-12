using System;
using PixelPlay.Foundation.Unity.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>
    /// Demonstrates local profile registration, profile selection, save, and reset with JSON save slots.
    /// This is local profile management only; it does not authenticate users or protect data from tampering.
    /// </summary>
    /// <remarks>
    /// Save data must be a plain serializable data class. Unity's JSON serializer stores supported fields,
    /// including primitives, strings, enums, serializable nested DTOs, and supported arrays/lists.
    /// It does not store properties, static fields, dictionaries, or durable scene-object references.
    /// </remarks>
    public sealed class SaveSlotsQuickStart : MonoBehaviour
    {
        /// <summary>Serializable example snapshot kept for each local profile.</summary>
        [Serializable]
        private sealed class DemoSaveData
        {
            public string playerName;
            public int level;
            public int score;
        }

        private const string SaveFolder = "PixelPlayFoundation/Saves";
        private const string ProfileSlotPrefix = "profile_";
        private const int SchemaVersion = 1;

        [SerializeField, Tooltip("TMP input field used by the Register User and Login User buttons.")]
        private TMP_InputField usernameInput;
        [SerializeField, Tooltip("TextMeshPro label that displays the active profile's saved data and save state.")]
        private TextMeshProUGUI userDataLabel;
        [SerializeField, Tooltip("Invoked with progress, success, and failure messages for UI feedback.")]
        private UnityEvent<string> onStatusChanged = new UnityEvent<string>();
        [SerializeField, Tooltip("Invoked when the active local profile changes; an empty value means logged out.")]
        private UnityEvent<string> onActiveUserChanged = new UnityEvent<string>();

        private PersistentJsonSaveSlotStore<DemoSaveData> _store;
        private DemoSaveData _currentData;
        private string _currentSlotId;
        private string _currentUserName;
        private bool _hasUnsavedChanges;

        private PersistentJsonSaveSlotStore<DemoSaveData> Store
        {
            get
            {
                if (_store == null)
                    _store = new PersistentJsonSaveSlotStore<DemoSaveData>(SaveFolder);
                return _store;
            }
        }

        /// <summary>Gets the active local profile name, or an empty string while logged out.</summary>
        public string CurrentUserName => _currentUserName ?? string.Empty;

        /// <summary>Gets whether a local profile is currently loaded.</summary>
        public bool IsLoggedIn => _currentData != null;

        /// <summary>Gets the active profile's current level, or zero when logged out.</summary>
        public int CurrentLevel => _currentData == null ? 0 : _currentData.level;

        /// <summary>Gets the active profile's current score, or zero when logged out.</summary>
        public int CurrentScore => _currentData == null ? 0 : _currentData.score;

        /// <summary>Gets the event used to update a status label.</summary>
        public UnityEvent<string> OnStatusChanged => onStatusChanged;

        /// <summary>Gets the event raised when a profile is loaded or logged out.</summary>
        public UnityEvent<string> OnActiveUserChanged => onActiveUserChanged;

        /// <summary>Assigns the TextMeshPro label used to display current profile data.</summary>
        public void SetUserDataLabel(TextMeshProUGUI value)
        {
            userDataLabel = value;
            RefreshUserDataDisplay();
        }

        /// <summary>Refreshes the user data label from the active in-memory profile.</summary>
        public void RefreshUserDataDisplay()
        {
            if (userDataLabel == null)
                return;

            if (_currentData == null)
            {
                userDataLabel.text = "No local profile loaded.\nRegister or log in to view profile data.";
                return;
            }

            userDataLabel.text =
                $"User: {_currentUserName}\n" +
                $"Player name: {_currentData.playerName ?? string.Empty}\n" +
                $"Level: {_currentData.level}\n" +
                $"Score: {_currentData.score}\n" +
                $"Save state: {(_hasUnsavedChanges ? "Unsaved changes" : "Saved")}";
        }

        /// <summary>Registers the username in the assigned TMP input field and logs that local profile in.</summary>
        public void RegisterUser()
        {
            RegisterUser(usernameInput == null ? string.Empty : usernameInput.text);
        }

        /// <summary>Creates a new local profile and makes it the active profile.</summary>
        /// <param name="userName">Unique local profile name.</param>
        /// <returns>True when the profile was created and loaded.</returns>
        public bool RegisterUser(string userName)
        {
            if (!TryNormalizeUserName(userName, out string normalizedName))
                return false;

            string slotId = BuildProfileSlotId(normalizedName);
            try
            {
                if (Store.HasSlot(slotId))
                {
                    ReportStatus($"Local profile '{normalizedName}' already exists. Log in to load it.");
                    return false;
                }

                DemoSaveData data = CreateDefaultData(normalizedName);
                Store.Save(slotId, data, SchemaVersion);
                SetCurrentProfile(slotId, normalizedName, data);
                ReportStatus($"Local profile '{normalizedName}' registered and loaded.");
                return true;
            }
            catch (Exception exception)
            {
                ReportError($"Could not register local profile: {exception.Message}");
                return false;
            }
        }

        /// <summary>Loads the local profile named in the assigned TMP input field.</summary>
        public void LoginUser()
        {
            LoginUser(usernameInput == null ? string.Empty : usernameInput.text);
        }

        /// <summary>Loads a previously registered local profile by name.</summary>
        /// <param name="userName">Local profile name.</param>
        /// <returns>True when the profile was found and loaded.</returns>
        public bool LoginUser(string userName)
        {
            if (!TryNormalizeUserName(userName, out string normalizedName))
                return false;

            string slotId = BuildProfileSlotId(normalizedName);
            try
            {
                if (!Store.TryLoad(slotId, out DemoSaveData data, out int schemaVersion))
                {
                    ReportStatus($"Local profile '{normalizedName}' was not found. Register it first.");
                    return false;
                }
                if (schemaVersion != SchemaVersion)
                {
                    ReportError($"Profile schema {schemaVersion} is not supported by this sample (expected {SchemaVersion}).");
                    return false;
                }

                SetCurrentProfile(slotId, normalizedName, data);
                ReportStatus($"Local profile '{normalizedName}' loaded.");
                return true;
            }
            catch (Exception exception)
            {
                ReportError($"Could not load local profile: {exception.Message}");
                return false;
            }
        }

        /// <summary>Saves the active profile's current in-memory data to its JSON save slot.</summary>
        public void SaveCurrentUserData()
        {
            if (!RequireActiveProfile())
                return;

            try
            {
                Store.Save(_currentSlotId, _currentData, SchemaVersion);
                _hasUnsavedChanges = false;
                RefreshUserDataDisplay();
                ReportStatus($"Saved data for '{_currentUserName}'.");
            }
            catch (Exception exception)
            {
                ReportError($"Could not save profile data: {exception.Message}");
            }
        }

        /// <summary>Resets the active profile to its defaults and immediately saves those defaults.</summary>
        public void ResetCurrentUserData()
        {
            if (!RequireActiveProfile())
                return;

            _currentData = CreateDefaultData(_currentUserName);
            _hasUnsavedChanges = true;
            RefreshUserDataDisplay();
            SaveCurrentUserData();
        }

        /// <summary>Updates the active profile's level in memory; call Save Current User Data to persist it.</summary>
        public void SetCurrentLevel(int value)
        {
            if (value < 1)
            {
                ReportError("Level must be at least one.");
                return;
            }
            if (RequireActiveProfile())
            {
                _currentData.level = value;
                MarkDataChanged();
            }
        }

        /// <summary>Updates the active profile's score in memory; call Save Current User Data to persist it.</summary>
        public void SetCurrentScore(int value)
        {
            if (RequireActiveProfile())
            {
                _currentData.score = value;
                MarkDataChanged();
            }
        }

        /// <summary>Updates the active profile's display name in memory; call Save Current User Data to persist it.</summary>
        public void SetCurrentPlayerName(string value)
        {
            if (RequireActiveProfile())
            {
                _currentData.playerName = value ?? string.Empty;
                MarkDataChanged();
            }
        }

        /// <summary>Clears the active local profile from memory without deleting its save data.</summary>
        public void LogoutUser()
        {
            _currentSlotId = null;
            _currentUserName = null;
            _currentData = null;
            _hasUnsavedChanges = false;
            RefreshUserDataDisplay();
            onActiveUserChanged?.Invoke(string.Empty);
            ReportStatus("Logged out of the local profile.");
        }

        private static DemoSaveData CreateDefaultData(string userName)
        {
            return new DemoSaveData { playerName = userName, level = 1, score = 0 };
        }

        private static string BuildProfileSlotId(string userName) => ProfileSlotPrefix + userName.ToUpperInvariant();

        private bool TryNormalizeUserName(string userName, out string normalizedName)
        {
            normalizedName = userName == null ? string.Empty : userName.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                ReportError("Enter a username first.");
                return false;
            }
            if (normalizedName.Length > 32)
            {
                ReportError("Username must be 32 characters or fewer.");
                return false;
            }
            return true;
        }

        private void SetCurrentProfile(string slotId, string userName, DemoSaveData data)
        {
            _currentSlotId = slotId;
            _currentUserName = userName;
            _currentData = data;
            _hasUnsavedChanges = false;
            RefreshUserDataDisplay();
            onActiveUserChanged?.Invoke(userName);
        }

        private void MarkDataChanged()
        {
            _hasUnsavedChanges = true;
            RefreshUserDataDisplay();
        }

        private bool RequireActiveProfile()
        {
            if (_currentData != null)
                return true;
            ReportError("Register or log in to a local profile first.");
            return false;
        }

        private void ReportStatus(string message)
        {
            Debug.Log(message, this);
            onStatusChanged?.Invoke(message);
        }

        private void ReportError(string message)
        {
            Debug.LogWarning(message, this);
            onStatusChanged?.Invoke(message);
        }
    }
}
