// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Log messages for <see cref="Capturer"/>.
/// </summary>
internal static partial class CapturerLogging
{
    [LoggerMessage(LogLevel.Information, "Starting capture '{Name}' ({CaptureId})")]
    internal static partial void StartingCapture(this ILogger<Capturer> logger, CaptureName name, CaptureId captureId);

    [LoggerMessage(LogLevel.Information, "Stopping capture {CaptureId}")]
    internal static partial void StoppingCapture(this ILogger<Capturer> logger, CaptureId captureId);

    [LoggerMessage(LogLevel.Error, "The declaration of capture '{Name}' ({CaptureId}) is invalid: {Errors}")]
    internal static partial void CaptureDeclarationInvalid(this ILogger<Capturer> logger, CaptureName name, CaptureId captureId, string errors);

    [LoggerMessage(LogLevel.Error, "Capture cycle for '{Name}' ({CaptureId}) failed")]
    internal static partial void CaptureCycleFailed(this ILogger<Capturer> logger, Exception exception, CaptureName name, CaptureId captureId);

    [LoggerMessage(LogLevel.Warning, "Skipping an item without a value for key property '{KeyProperty}' for capture {CaptureId}")]
    internal static partial void SkippingItemWithoutKey(this ILogger<Capturer> logger, CaptureId captureId, string keyProperty);

    [LoggerMessage(LogLevel.Error, "Capture {CaptureId} references the event type '{EventType}' which is not registered")]
    internal static partial void UnknownEventType(this ILogger<Capturer> logger, CaptureId captureId, string eventType);

    [LoggerMessage(LogLevel.Error, "Appending captured events for '{Name}' ({CaptureId}) failed: {Errors}")]
    internal static partial void AppendingCapturedEventsFailed(this ILogger<Capturer> logger, CaptureName name, CaptureId captureId, string errors);
}
