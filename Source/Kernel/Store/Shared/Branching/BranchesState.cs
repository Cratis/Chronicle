// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Branching;

/// <summary>
/// Represents the state used to keep track of branches.
/// </summary>
public class BranchesState
{
    /// <summary>
    /// The name of the storage provider used for working with this type of state.
    /// </summary>
    public const string StorageProvider = "branches-state";

    readonly List<BranchState> _branches = new();

    /// <summary>
    /// Gets all the branches.
    /// </summary>
    public IList<BranchState> Branches => _branches;

    /// <summary>
    /// Initializes a new instance of the <see cref="BranchesState"/> class.
    /// </summary>
    public BranchesState()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BranchesState"/> class.
    /// </summary>
    /// <param name="branches">The branches.</param>
    public BranchesState(IEnumerable<BranchState> branches)
    {
        _branches.AddRange(branches);
    }
}
