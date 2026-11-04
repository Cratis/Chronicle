// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics;

internal static partial class MixedReleaseCallDiagnosticsLogging
{
    [LoggerMessage(LogLevel.Error, "Mixed-release grain call failed: {Interface}.{Method}; failure: {FailureType}; local release: {LocalRelease}; cluster releases: {ClusterReleases}")]
    internal static partial void MixedReleaseCallFailed(this ILogger<MixedReleaseCallDiagnostics> logger, string @interface, string method, string failureType, string localRelease, string clusterReleases);

    [LoggerMessage(LogLevel.Error, "Mixed-release grain call diagnostics: further failures suppressed after 32 distinct interface/method pairs")]
    internal static partial void FurtherFailuresSuppressed(this ILogger<MixedReleaseCallDiagnostics> logger);
}
