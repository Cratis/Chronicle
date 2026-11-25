// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The folded state of the policy of one subject.
/// </summary>
/// <param name="Subject">The opaque subject the policy is for.</param>
/// <param name="Status">Whether the policy admits the subject's links.</param>
/// <param name="Entries">What the subject may do, one entry per direction.</param>
/// <param name="ChangedBy">The identity that last acted on the policy.</param>
public record FederationPolicyState(
    string Subject,
    FederationPolicyStatus Status,
    IReadOnlyList<FederationPolicyEntry> Entries,
    string ChangedBy)
{
    /// <summary>
    /// Find the entry of a direction.
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <returns>The entry, or null when the policy allows nothing in that direction.</returns>
    public FederationPolicyEntry? EntryFor(FederationDirection direction) => Entries.FirstOrDefault(_ => _.Direction == direction);
}
