using System;

namespace QQGameRes
{
    /// <summary>
    /// Represents a collection of resource items.
    /// </summary>
    public interface ResourceGroup
    {
        /// <summary>
        /// Gets the name of the resource group.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the resource items in this group.
        /// </summary>
        ResourceEntry[] Entries { get; }
    }
}
