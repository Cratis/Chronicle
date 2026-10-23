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

    [LoggerMessage(LogLevel.Trace, "Current system version: {Version}")]
    internal static partial void CurrentSystemVersion(this ILogger<PatchManager> logger, SemanticVersion version);

    [LoggerMessage(LogLevel.Debug, "Found {Count} patches to apply")]
    internal static partial void FoundPatchesToApply(this ILogger<PatchManager> logger, int count);

    [LoggerMessage(LogLevel.Information, "Applying patch {PatchName} for version {Version} with kernel {KernelVersion}")]
    internal static partial void ApplyingPatch(this ILogger<PatchManager> logger, string patchName, SemanticVersion version, SemanticVersion kernelVersion);

    [LoggerMessage(LogLevel.Information, "Successfully applied patch {PatchName}")]
    internal static partial void PatchAppliedSuccessfully(this ILogger<PatchManager> logger, string patchName);

    [LoggerMessage(LogLevel.Error, "Failed to apply patch {PatchName}")]
    internal static partial void PatchApplicationFailed(this ILogger<PatchManager> logger, string patchName, Exception exception);

    [LoggerMessage(LogLevel.Trace, "Updated system version to {Version}")]
    internal static partial void UpdatedSystemVersion(this ILogger<PatchManager> logger, SemanticVersion version);

    [LoggerMessage(LogLevel.Debug, "Patch application process completed. Applied {AppliedCount} patches and skipped {SkippedCount} patches")]
    internal static partial void PatchApplicationCompleted(this ILogger<PatchManager> logger, int appliedCount, int skippedCount);

    [LoggerMessage(LogLevel.Warning, "Skipping patches on kernel {KernelVersion}: kernel {RecordedVersion} has already patched this store")]
    internal static partial void SkippingPatchesForNewerKernel(this ILogger<PatchManager> logger, SemanticVersion kernelVersion, SemanticVersion recordedVersion);

    [LoggerMessage(LogLevel.Warning, "Kernel version {KernelVersion} is unknown; bypassing the patch version gate without recording a kernel version")]
    internal static partial void UnknownKernelVersion(this ILogger<PatchManager> logger, SemanticVersion kernelVersion);
}
