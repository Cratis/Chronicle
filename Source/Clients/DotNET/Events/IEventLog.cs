// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Branching;

namespace Aksio.Cratis.Events;

/// <summary>
/// Defines the client event log.
/// </summary>
public interface IEventLog : IEventSequence
{
    /// <summary>
    /// Start a branch for a specific <see cref="BranchTypeId"/>.
    /// </summary>
    /// <param name="branchTypeId">Optional branch type to start. If not specified, it will be set to NotSpecified.</param>
    /// <param name="tags">Optional tags to associate with the branch.</param>
    /// <returns><see cref="IBranch"/>.</returns>
    Task<IBranch> Branch(BranchTypeId? branchTypeId = default, IDictionary<string, string>? tags = default);

    /// <summary>
    /// Get branches of a specific <see cref="BranchTypeId"/>.
    /// </summary>
    /// <param name="branchTypeId">Type to get for.</param>
    /// <returns>Collection of <see cref="IBranch"/>.</returns>
    Task<IEnumerable<BranchDescriptor>> GetBranchesFor(BranchTypeId branchTypeId);
}
