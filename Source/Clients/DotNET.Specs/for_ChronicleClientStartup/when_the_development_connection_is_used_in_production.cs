// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.for_ChronicleClientStartup;

public class when_the_development_connection_is_used_in_production : given.a_chronicle_client_startup
{
    void Establish()
    {
        _environment.EnvironmentName.Returns(Environments.Production);
        _options.ConnectionString = ChronicleConnectionString.Development;
    }

    async Task Because() => await _startup.StartAsync(CancellationToken.None);

    [Fact] void should_warn() => _logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("development connection"));
    [Fact] void should_name_the_configuration_key() => _logger.Entries.ShouldContain(e => e.Message.Contains("Cratis:Chronicle:ConnectionString"));
    [Fact] void should_not_log_the_credentials() => _logger.Entries.ShouldNotContain(e => e.Message.Contains(ChronicleConnectionString.DevelopmentClientSecret));
}
