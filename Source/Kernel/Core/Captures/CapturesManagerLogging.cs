// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
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

    [LoggerMessage(LogLevel.Error, "Failed subscribing capture '{Name}' ({CaptureId}) in namespace '{Namespace}'")]
    internal static partial void FailedSubscribingCaptureInNamespace(this ILogger<CapturesManager> logger, Exception exception, CaptureName name, CaptureId captureId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Error, "Failed recovering the subscription of capture '{Name}' ({CaptureId}) - it is retried on the next reconciliation")]
    internal static partial void FailedRecoveringCapture(this ILogger<CapturesManager> logger, Exception exception, CaptureName name, CaptureId captureId);
}
