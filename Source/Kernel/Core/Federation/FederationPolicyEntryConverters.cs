// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Converts <see cref="FederationPolicyEntry"/> to its wire contract.
/// </summary>
public static class FederationPolicyEntryConverters
{
    /// <summary>
    /// Convert to the contract.
    /// </summary>
    /// <param name="entry">The entry to convert.</param>
    /// <returns>The contract.</returns>
    public static Contracts.Federation.FederationPolicyEntry ToContract(this FederationPolicyEntry entry) => new()
    {
        Direction = (Contracts.Federation.FederationDirection)entry.Direction,
        EventStore = entry.EventStore,
        Namespace = entry.Namespace,
        AllPublic = entry.AllPublic,
        EventTypes = [.. entry.EventTypes]
    };
}
