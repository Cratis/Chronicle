// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Branching;

/// <summary>
/// Represents a descriptor of a branch.
/// </summary>
/// <param name="Type">Type of branch.</param>
/// <param name="Identifier">Identifier identifying the branch uniquely.</param>
/// <param name="Started">The date and time when the branch was started.</param>
/// <param name="From">The <see cref="EventSequenceNumber"/> the branch was started from.</param>
/// <param name="Tags">Any tags associated with the branch.</param>
public record BranchDescriptor(BranchTypeId Type, BranchId Identifier, DateTimeOffset Started, EventSequenceNumber From, IDictionary<string, string> Tags);
