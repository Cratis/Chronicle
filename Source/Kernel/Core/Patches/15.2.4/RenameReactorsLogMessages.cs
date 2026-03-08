// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Holds log messages for <see cref="RenameReactors"/>.
/// </summary>
internal static partial class RenameReactorsLogMessages
{
    [LoggerMessage(LogLevel.Information, "Starting RenameReactors patch - removing 'Grains' from reactor names")]
    internal static partial void StartingPatch(this ILogger<RenameReactors> logger);

    [LoggerMessage(LogLevel.Information, "Found {Count} reactors to rename")]
    internal static partial void FoundReactorsToRename(this ILogger<RenameReactors> logger, int count);

    [LoggerMessage(LogLevel.Information, "Renaming reactor from {OldId} to {NewId}")]
    internal static partial void RenamingReactor(this ILogger<RenameReactors> logger, ReactorId oldId, ReactorId newId);

    [LoggerMessage(LogLevel.Information, "Completed RenameReactors patch")]
    internal static partial void PatchCompleted(this ILogger<RenameReactors> logger);

    [LoggerMessage(LogLevel.Information, "Starting RenameReactors patch rollback - adding 'Grains' back to reactor names")]
    internal static partial void StartingRollback(this ILogger<RenameReactors> logger);

    [LoggerMessage(LogLevel.Information, "Restoring reactor name from {OldId} to {NewId}")]
    internal static partial void RestoringReactorName(this ILogger<RenameReactors> logger, ReactorId oldId, ReactorId newId);

    [LoggerMessage(LogLevel.Information, "Completed RenameReactors patch rollback")]
    internal static partial void RollbackCompleted(this ILogger<RenameReactors> logger);
}
