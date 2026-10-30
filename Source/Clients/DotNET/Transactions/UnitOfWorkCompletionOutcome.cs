// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>The outcome of completing a unit of work through a completion claim.</summary>
public enum UnitOfWorkCompletionOutcome
{
    /// <summary>The unit of work was committed and every event was appended.</summary>
    Committed = 0,

    /// <summary>The commit completed but was rejected: conflicts, constraint violations or append errors.</summary>
    Failed = 1,

    /// <summary>The unit of work was rolled back; nothing was appended.</summary>
    RolledBack = 2,

    /// <summary>A technical failure means it is not known whether any events were appended. See <see cref="UnitOfWorkCompletionResult.Exception"/>.</summary>
    Unknown = 3
}
