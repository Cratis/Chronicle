// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasing.given;

public class a_namespace_casing : Specification
{
    protected INamespaces _namespaces;
    protected NamespaceCasing _casing;
    protected RecordingLogger _logger;
    protected EventStoreNamespaceName _first;
    protected EventStoreNamespaceName _second;

    void Establish()
    {
        var factory = Substitute.For<IGrainFactory>();
        _namespaces = Substitute.For<INamespaces>();
        factory.GetGrain<INamespaces>(Arg.Any<string>(), Arg.Any<string?>()).Returns(_namespaces);
        _logger = new RecordingLogger();
        _casing = new(factory, _logger);
    }

    protected sealed class RecordingLogger : ILogger<NamespaceCasing>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
