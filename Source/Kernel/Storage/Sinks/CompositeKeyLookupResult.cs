// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The result of a structured composite-key lookup.
/// </summary>
/// <param name="Outcome">The lookup outcome.</param>
/// <param name="Instance">The matching instance, only present when found.</param>
public record CompositeKeyLookupResult(CompositeKeyLookupOutcome Outcome, ExpandoObject? Instance)
{
    /// <summary>Gets the result for a missing instance.</summary>
    public static readonly CompositeKeyLookupResult NotFound = new(CompositeKeyLookupOutcome.NotFound, null);

    /// <summary>Gets the result for an ambiguous legacy key.</summary>
    public static readonly CompositeKeyLookupResult AmbiguousLegacyKey = new(CompositeKeyLookupOutcome.AmbiguousLegacyKey, null);

    /// <summary>Gets the result for a foreign composite-key stamp.</summary>
    public static readonly CompositeKeyLookupResult KeyCollision = new(CompositeKeyLookupOutcome.KeyCollision, null);

    /// <summary>Gets the result for a read model without this capability.</summary>
    public static readonly CompositeKeyLookupResult NotSupported = new(CompositeKeyLookupOutcome.NotSupported, null);

    /// <summary>
    /// Creates the result for a matching instance.
    /// </summary>
    /// <param name="instance">The matching instance.</param>
    /// <returns>The found result.</returns>
    public static CompositeKeyLookupResult Found(ExpandoObject instance) => new(CompositeKeyLookupOutcome.Found, instance);
}
