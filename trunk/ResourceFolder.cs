using System;

namespace QQGameRes
{
    /// <summary>
    /// Represents a collection of resource entries in the same location.
    /// </summary>
    public interface ResourceFolder
    {
        /// <summary>
        /// Gets the name of the resource folder.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the resource entries in this folder.
        /// </summary>
        ResourceEntry[] Entries { get; }
    }
}
