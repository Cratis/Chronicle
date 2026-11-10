// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider;

public class when_reading_the_template_of_a_generated_logger_message : Specification
{
    TemplateReadingLogger _logger;

    void Establish() => _logger = new();

    void Because() => _logger.ReadingFailed(new TimeoutException(), 42);

    [Fact] void should_read_the_template() => _logger.Template.ShouldEqual(StorageLogging.ReadingFailedTemplate);

    sealed class TemplateReadingLogger : ILogger
    {
        public string? Template { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Template = RepeatedFailureLoggerProvider.MessageTemplateOf(state, eventId);
        }
    }
}
