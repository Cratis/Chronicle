// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.Authentication.for_StoreEntitlementsStartupNotice;

sealed class RecordingLogger : ILogger<StoreEntitlementsStartupNotice>
{
    public List<LogLevel> Levels { get; } = [];

    public List<string?> Events { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Levels.Add(logLevel);
        Events.Add(eventId.Name);
    }
}
