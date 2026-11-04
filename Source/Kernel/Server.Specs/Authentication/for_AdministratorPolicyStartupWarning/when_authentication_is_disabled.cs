// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_AdministratorPolicyStartupWarning;

public class when_authentication_is_disabled : Specification
{
    readonly RecordingLogger _logger = new();

    async Task Because()
    {
        var options = Options.Create(new ChronicleOptions { Authentication = new Configuration.Authentication { Enabled = false } });
        var warning = new AdministratorPolicyStartupWarning(options, _logger);
        await warning.StartAsync(CancellationToken.None);
        await warning.StopAsync(CancellationToken.None);
    }

    [Fact] void should_warn_once_at_startup() => _logger.Entries.Single().Level.ShouldEqual(LogLevel.Warning);
    [Fact] void should_describe_the_authentication_off_bypass() => _logger.Entries.Single().Message.ShouldContain("administrator authorization allows all callers");

    sealed class RecordingLogger : ILogger<AdministratorPolicyStartupWarning>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
