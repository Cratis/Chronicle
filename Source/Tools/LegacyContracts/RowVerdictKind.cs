// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts;

enum RowVerdictKind
{
    /// <summary>
    /// Every declared executable proof passed.
    /// </summary>
    Green = 0,

    /// <summary>
    /// The row has no valid owner issue.
    /// </summary>
    Unowned = 1,

    /// <summary>
    /// A required job or executable proof failed.
    /// </summary>
    Red = 2,

    /// <summary>
    /// Every owner completed without wiring the pending proofs.
    /// </summary>
    StalePending = 3,

    /// <summary>
    /// Proofs remain unwired while a valid owner issue is open.
    /// </summary>
    Pending = 4,

    /// <summary>
    /// A required job skipped its compatibility check.
    /// </summary>
    Skipped = 5
}
