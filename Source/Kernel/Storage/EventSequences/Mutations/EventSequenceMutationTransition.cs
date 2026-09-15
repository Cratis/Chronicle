// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies a pure event sequence mutation state transition.
/// </summary>
public enum EventSequenceMutationTransition
{
    /// <summary>No transition was specified.</summary>
    Unspecified = 0,

    /// <summary>Begins applying a reserved mutation.</summary>
    BeginApplying = 1,

    /// <summary>Begins verification after applying.</summary>
    BeginVerifying = 2,

    /// <summary>Blocks an applying or verifying mutation.</summary>
    Block = 3,

    /// <summary>Resumes a blocked mutation at its recorded source phase.</summary>
    Resume = 4,

    /// <summary>Commits the source atomically with no repair required.</summary>
    CommitSourceWithoutRepair = 5,

    /// <summary>Commits the source atomically with repair pending.</summary>
    CommitSourceWithRepair = 6,

    /// <summary>Begins dispatching pending repair.</summary>
    BeginRepairDispatch = 7,

    /// <summary>Marks dispatched repair as accepted.</summary>
    AcceptRepair = 8,

    /// <summary>Marks dispatched repair as having unknown outcome.</summary>
    MarkRepairUnknown = 9
}
