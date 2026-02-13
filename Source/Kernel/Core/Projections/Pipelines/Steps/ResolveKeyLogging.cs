// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ResolveKeyLogging
{
    [LoggerMessage(LogLevel.Debug, "Resolving key for event with sequence number {SequenceNumber}")]
    internal static partial void ResolvingKey(this ILogger<ResolveKey> logger, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Information, "Key resolution deferred for event {SequenceNumber} - projection '{ProjectionId}' at path '{Path}' - creating future")]
    internal static partial void KeyResolutionDeferred(this ILogger<ResolveKey> logger, ulong sequenceNumber, string projectionId, string path);
}
