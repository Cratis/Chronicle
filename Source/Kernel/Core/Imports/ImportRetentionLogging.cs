// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Imports;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class ImportRetentionLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Sweeping imports for retention failed in event store '{EventStore}', namespace '{Namespace}'")]
    internal static partial void SweepFailed(this ILogger<ImportRetention> logger, string eventStore, string @namespace, Exception exception);
}
