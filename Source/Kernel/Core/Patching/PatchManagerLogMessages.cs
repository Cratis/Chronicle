// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Holds log messages for <see cref="PatchManager"/>.
/// </summary>
internal static partial class PatchManagerLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting patch application process")]
    internal static partial void StartingPatchApplication(this ILogger<PatchManager> logger);

    [LoggerMessage(LogLevel.Debug, "Current system version: {Version}")]
    internal static partial void CurrentSystemVersion(this ILogger<PatchManager> logger, SemanticVersion version);

    [LoggerMessage(LogLevel.Debug, "No patches to apply")]
    internal static partial void NoPatchesToApply(this ILogger<PatchManager> logger);

    [LoggerMessage(LogLevel.Information, "Found {Count} patches to apply")]
    internal static partial void FoundPatchesToApply(this ILogger<PatchManager> logger, int count);

    [LoggerMessage(LogLevel.Debug, "Patch {PatchName} already applied, skipping")]
    internal static partial void PatchAlreadyApplied(this ILogger<PatchManager> logger, string patchName);

    [LoggerMessage(LogLevel.Information, "Applying patch {PatchName} for version {Version}")]
    internal static partial void ApplyingPatch(this ILogger<PatchManager> logger, string patchName, SemanticVersion version);

    [LoggerMessage(LogLevel.Information, "Successfully applied patch {PatchName}")]
    internal static partial void PatchAppliedSuccessfully(this ILogger<PatchManager> logger, string patchName);

    [LoggerMessage(LogLevel.Error, "Failed to apply patch {PatchName}")]
    internal static partial void PatchApplicationFailed(this ILogger<PatchManager> logger, string patchName, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Updated system version to {Version}")]
    internal static partial void UpdatedSystemVersion(this ILogger<PatchManager> logger, SemanticVersion version);

    [LoggerMessage(LogLevel.Debug, "Patch application process completed")]
    internal static partial void PatchApplicationCompleted(this ILogger<PatchManager> logger);
}
