using System;

namespace Devices.Common.Exceptions
{
    /// <summary>
    /// Exception thrown when no DAL Features could be found (core and extended).
    /// </summary>
    public sealed class DALFeatureLoadException : Exception
    {
        public string FeatureDirectoryPath { get; }

        public DALFeatureLoadException() : base() { }

        public DALFeatureLoadException(string featureDirectoryPath, string message)
            : base(message) => (FeatureDirectoryPath) = (featureDirectoryPath);
    }
}
