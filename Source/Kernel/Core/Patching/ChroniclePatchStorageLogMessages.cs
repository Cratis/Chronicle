// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Holds log messages for <see cref="ChroniclePatchStorage"/>.
/// </summary>
internal static partial class ChroniclePatchStorageLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Patch {PatchName} did not reach the quarantined event stores or namespaces {Resources}, so it is not recorded as applied and runs again at the next startup")]
    internal static partial void PatchDeferred(this ILogger<ChroniclePatchStorage> logger, string patchName, string resources);
}
