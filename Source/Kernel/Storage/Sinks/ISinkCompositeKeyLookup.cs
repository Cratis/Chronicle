// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Optional sink capability for looking up a materialized read model by its structured composite key.
/// </summary>
public interface ISinkCompositeKeyLookup
{
    /// <summary>
    /// Finds an instance in the primary container using normalized composite key parts.
    /// </summary>
    /// <param name="key">The key containing normalized parts in an expando object.</param>
    /// <returns>The lookup outcome and, when found, the instance.</returns>
    Task<CompositeKeyLookupResult> FindByCompositeKey(Key key);
}
