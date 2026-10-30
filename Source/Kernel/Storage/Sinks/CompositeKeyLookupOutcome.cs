// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The outcome of looking up a materialized read model by composite key.
/// </summary>
public enum CompositeKeyLookupOutcome
{
    /// <summary>No instance matches the key.</summary>
    NotFound = 0,

    /// <summary>One instance matches the key.</summary>
    Found = 1,

    /// <summary>The legacy key cannot identify one instance safely.</summary>
    AmbiguousLegacyKey = 2,

    /// <summary>Another composite key owns the legacy id.</summary>
    KeyCollision = 3,

    /// <summary>The read model or sink does not support composite-key lookup.</summary>
    NotSupported = 4
}
