// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Branching;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Branching;

/// <summary>
/// Defines a system for maintaining branches.
/// </summary>
public interface IBranches : IGrainWithGuidKey
{
    /// <summary>
    /// Checkout the branch.
    /// </summary>
    /// <param name="branchTypeId">Type to checkout.</param>
    /// <param name="from">Optional <see cref="EventSequenceNumber"/> the branch was started from. If not specified, it will branch from current tail.</param>
    /// <param name="labels">Optional labels to associate with the branch.</param>
    /// <returns>The <see cref="BranchId"/> checked out.</returns>
    Task<BranchId> Checkout(BranchTypeId branchTypeId, EventSequenceNumber? from = default, IDictionary<string, string>? labels = default);

    /// <summary>
    /// Concludes a branch by its identifier.
    /// </summary>
    /// <param name="branchId"><see cref="BranchId"/> to conclude.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Any artifacts produced in the branch will be lost if the branch is not merged.
    /// </remarks>
    Task Conclude(BranchId branchId);

    /// <summary>
    /// Get branches of a specific <see cref="BranchTypeId"/>.
    /// </summary>
    /// <param name="branchTypeId">Type to get for.</param>
    /// <returns>Collection of <see cref="IBranch"/>.</returns>
    Task<IEnumerable<BranchDescriptor>> GetFor(BranchTypeId branchTypeId);
}
