// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Branching;

/// <summary>
/// Represents the state for a branch.
/// </summary>
/// <param name="Identifier">The <see cref="BranchId">identifier</see>.</param>
/// <param name="Type">The <see cref="BranchTypeId">type</see>.</param>
/// <param name="From">The <see cref="EventSequenceNumber">sequence number</see> the branch is from.</param>
/// <param name="Started">The <see cref="DateTimeOffset"/> representing the start time of the branch.</param>
/// <param name="Labels">A dictionary of labels associated with the branch.</param>
public record BranchState(
    BranchId Identifier,
    BranchTypeId Type,
    EventSequenceNumber From,
    DateTimeOffset Started,
    IDictionary<string, string> Labels);
