// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchema
{
    volatile Cached<MetadataReach>? _metadataReferencesCache;
    volatile Cached<MetadataReach>? _metadataGraphCache;
    volatile Cached<bool>? _hasProtectedValueMetadataCache;

    /// <summary>
    /// Gets the memoized conservative reference graph for metadata, independently of type selection.
    /// </summary>
    internal MetadataReach MetadataReferences => (_metadataReferencesCache ??= new(ReachMetadata(includeChildren: false))).Value;

    /// <summary>
    /// Gets memoized metadata reach through references and supported children, sharing one traversal budget.
    /// </summary>
    internal MetadataReach MetadataGraph => (_metadataGraphCache ??= new(ReachMetadata(includeChildren: true))).Value;

    /// <summary>
    /// Gets or sets the memoized answer to whether a converted value is protected.
    /// A getter value of <see langword="null"/> means it has not been computed yet.
    /// </summary>
    internal bool? CachedHasProtectedValueMetadata
    {
        get => _hasProtectedValueMetadataCache is { } cache ? cache.Value : null;
        set => _hasProtectedValueMetadataCache = value is { } computed ? new Cached<bool>(computed) : null;
    }
}
