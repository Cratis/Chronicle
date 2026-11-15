// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.for_ChronicleAspNetCoreStartupDiagnostics.given;

public class startup_diagnostics : Specification
{
    internal ChronicleUseMarker _marker;
    protected IEventStoreNamespaceResolver _resolver;
    protected CapturingLogger _logger;

    void Establish()
    {
        _marker = new();
        _resolver = Substitute.For<IEventStoreNamespaceResolver>();
        _logger = new();
    }

    internal ChronicleAspNetCoreStartupDiagnostics Create() => new(_marker, _resolver, "x-the-header");

    protected sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
