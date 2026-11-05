// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_AdministratorPolicyStartupWarning;

public class when_an_external_administrator_mapping_is_configured_with_authentication_disabled : Specification
{
    readonly RecordingLogger _logger = new();

    async Task Because()
    {
        var options = Options.Create(new ChronicleOptions
        {
            Authentication = new Configuration.Authentication
            {
                Enabled = false,
                Authority = "https://identity.example",
                ExternalAdministratorClaim = new ExternalAdministratorClaim { Type = "roles", Value = "secret-admin-value" }
            }
        });
        var warning = new AdministratorPolicyStartupWarning(options, _logger);
        await warning.StartAsync(CancellationToken.None);
    }

    [Fact] void should_warn_the_authentication_is_off_and_the_mapping_has_no_effect() => _logger.Entries.Count.ShouldEqual(2);
    [Fact] void should_describe_the_ignored_mapping() => _logger.Entries.Exists(_ => _.Message.Contains("has no effect")).ShouldBeTrue();
    [Fact] void should_not_log_the_value() => _logger.Entries.Exists(_ => _.Message.Contains("secret-admin-value")).ShouldBeFalse();

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
