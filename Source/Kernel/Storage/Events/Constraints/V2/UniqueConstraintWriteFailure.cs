// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

/// <summary>
/// Defines expected write refusals without disclosing claim or key material.
/// </summary>
public enum UniqueConstraintWriteFailure
{
    /// <summary>
    /// No authority has been explicitly initialized.
    /// </summary>
    MissingAuthority = 0,

    /// <summary>
    /// The expected epoch, phase, run or revision does not match.
    /// </summary>
    AuthorityMismatch = 1,

    /// <summary>
    /// Another event source already owns the value in this scope.
    /// </summary>
    ConflictingClaim = 2,

    /// <summary>
    /// A transition would exhaust the revision range.
    /// </summary>
    RevisionExhausted = 3
}
