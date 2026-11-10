// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider.given;

internal static partial class StorageLogging
{
    internal const string ReadingFailedTemplate = "Reading {Grain} failed";

    [LoggerMessage(LogLevel.Error, ReadingFailedTemplate)]
    internal static partial void ReadingFailed(this ILogger logger, Exception exception, int grain);

    [LoggerMessage(LogLevel.Error, "Writing {Grain} failed")]
    internal static partial void WritingFailed(this ILogger logger, Exception exception, int grain);

    [LoggerMessage(LogLevel.Information, "Still going")]
    internal static partial void StillGoing(this ILogger logger);
}
