// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// A version-fenced schedule cursor in its storage scope.
/// </summary>
/// <param name="Token">The stable schedule token.</param>
/// <param name="LastMaterializedDue">The last covered epoch second.</param>
/// <param name="RegisteredRevision">The registration stamp.</param>
/// <param name="AppliedRevision">The last directive applied atomically.</param>
/// <param name="Version">The compare-and-set version.</param>
public record ScheduleCursor(
    ScheduleToken Token,
    long LastMaterializedDue,
    long RegisteredRevision,
    long AppliedRevision,
    long Version);
