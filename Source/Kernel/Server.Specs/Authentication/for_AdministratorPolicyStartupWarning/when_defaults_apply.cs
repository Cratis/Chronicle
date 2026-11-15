// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_AdministratorPolicyStartupWarning;

public class when_defaults_apply : Specification
{
    readonly RecordingLogger _logger = new();

    async Task Because()
    {
        var options = Options.Create(new ChronicleOptions { Authentication = new Configuration.Authentication { Enabled = true } });
        var warning = new AdministratorPolicyStartupWarning(options, _logger);
        await warning.StartAsync(CancellationToken.None);
    }

    [Fact] void should_warn_for_both_unconfigured_flags() => _logger.Entries.Count.ShouldEqual(2);
    [Fact] void should_name_both_flags() => string.Join(' ', _logger.Entries.Select(_ => _.Message)).ShouldContain("EnforceAdministratorPolicyForOperations");
    [Fact] void should_name_destructive_enforcement() => string.Join(' ', _logger.Entries.Select(_ => _.Message)).ShouldContain("EnforceDestructiveOperations");
    [Fact] void should_explain_the_next_major_default() => _logger.Entries.TrueForAll(_ => _.Level == LogLevel.Warning && _.Message.Contains("default becomes enforcing in the next major release")).ShouldBeTrue();

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
