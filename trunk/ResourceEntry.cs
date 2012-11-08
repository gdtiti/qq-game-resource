using System;
using System.IO;

namespace QQGameRes
{
    /// <summary>
    /// Represents a resource item with an associated name and stream.
    /// </summary>
    public interface ResourceEntry
    {
        /// <summary>
        /// Gets the name of the resource item.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the size of the resource item in bytes.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Opens the resource item for reading.
        /// </summary>
        /// <returns>A stream to read the resource from.</returns>
        Stream Open();
    }
}
