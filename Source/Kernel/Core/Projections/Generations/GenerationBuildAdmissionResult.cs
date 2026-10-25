// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Describes the outcome of candidate build admission.
/// </summary>
public enum GenerationBuildAdmissionResult
{
    /// <summary>The operation owns the candidate's fixed writer grant.</summary>
    Admitted = 0,

    /// <summary>The operational generation flag is off.</summary>
    Disabled = 1,

    /// <summary>The store has no established generation floor.</summary>
    Uninitialized = 2,

    /// <summary>The request does not identify the current materialization or this owner.</summary>
    StaleKey = 3,

    /// <summary>The execution inputs or storage provider cannot build generations.</summary>
    Unsupported = 4,

    /// <summary>The materialization has no serving binding.</summary>
    NotBound = 5,

    /// <summary>Another operation already has a candidate in flight.</summary>
    CandidateInFlight = 6,

    /// <summary>The reserved candidate was abandoned and requires a new operation.</summary>
    Abandoned = 7,

    /// <summary>The operation or reserved identity conflicts with durable state.</summary>
    Conflict = 8,

    /// <summary>The operation has no authority to acquire or reuse a writer grant.</summary>
    GrantRefused = 9
}
