// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Log messages for <see cref="CapturesManager"/>.
/// </summary>
internal static partial class CapturesManagerLogging
{
    [LoggerMessage(LogLevel.Error, "Failed resuming capture '{Name}' ({CaptureId})")]
    internal static partial void FailedResumingCapture(this ILogger<CapturesManager> logger, Exception exception, CaptureName name, CaptureId captureId);
}
