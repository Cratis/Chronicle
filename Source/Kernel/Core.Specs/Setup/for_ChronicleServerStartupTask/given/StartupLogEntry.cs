// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.given;

/// <summary>
/// A log entry the startup task wrote, by event rather than by its rendered text.
/// </summary>
/// <param name="Level">The <see cref="LogLevel"/>.</param>
/// <param name="Event">The name of the log message, which is the name of the logging method.</param>
/// <param name="Exception">The exception attached to the entry, if any.</param>
public record StartupLogEntry(LogLevel Level, string? Event, Exception? Exception);
