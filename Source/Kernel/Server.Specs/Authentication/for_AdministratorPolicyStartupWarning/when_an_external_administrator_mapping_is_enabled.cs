// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_AdministratorPolicyStartupWarning;

public class when_an_external_administrator_mapping_is_enabled : Specification
{
    readonly RecordingLogger _logger = new();

    async Task Because()
    {
        var options = Options.Create(new ChronicleOptions
        {
            Authentication = new Configuration.Authentication
            {
                Enabled = true,
                EnforceAdministratorPolicyForOperations = true,
                Authority = "https://identity.example",
                ExternalAdministratorClaim = new ExternalAdministratorClaim { Type = "roles", Value = "secret-admin-value" }
            }
        });
        var warning = new AdministratorPolicyStartupWarning(options, _logger);
        await warning.StartAsync(CancellationToken.None);
    }

    [Fact] void should_log_the_claim_type_once() => _logger.Entries.Single().Message.ShouldContain("claim type roles");
    [Fact] void should_log_at_information() => _logger.Entries.Single().Level.ShouldEqual(LogLevel.Information);
    [Fact] void should_not_log_the_value() => _logger.Entries.Single().Message.ShouldNotContain("secret-admin-value");

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
