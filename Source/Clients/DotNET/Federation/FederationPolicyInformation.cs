// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The policy of a hosted federation subject, as the kernel holds it.
/// </summary>
/// <param name="Subject">The opaque subject the policy is for.</param>
/// <param name="Status">Whether the policy currently lets the subject's links through.</param>
/// <param name="Entries">What the subject may do, one entry per direction that is set.</param>
/// <param name="ChangedBy">The identity that last acted on the policy.</param>
public record FederationPolicyInformation(
    string Subject,
    FederationPolicyStatus Status,
    IReadOnlyList<FederationPolicyEntry> Entries,
    string ChangedBy);
