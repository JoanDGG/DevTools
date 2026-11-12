using System;

namespace PixelPlay.Foundation.QrCodes
{
    /// <summary>Immutable QR module matrix produced by <see cref="QrCodeEncoder"/>.</summary>
    public sealed class QrCodeMatrix
    {
        private readonly bool[,] _modules;

        internal QrCodeMatrix(bool[,] modules, int version)
        {
            _modules = (bool[,])modules.Clone();
            Version = version;
        }

        /// <summary>Gets the QR version (1-5).</summary>
        public int Version { get; }

        /// <summary>Gets the number of modules along each side.</summary>
        public int ModuleCount => _modules.GetLength(0);

        /// <summary>Gets whether the module at the supplied top-left-origin coordinate is dark.</summary>
        public bool IsDark(int x, int y)
        {
            if (x < 0 || x >= ModuleCount)
                throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= ModuleCount)
                throw new ArgumentOutOfRangeException(nameof(y));
            return _modules[x, y];
        }
    }
}
