// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_an_authority_and_logging : given.a_factory_context
{
    RecordingLoggerFactory _loggers;

    void Establish() => _loggers = new RecordingLoggerFactory();

    void Because()
    {
        var options = Options(WithCredentials, new() { Authority = "https://idp.example.com/hostile-path-marker?hidden=value" });
        _result = TokenProviderFactory.Create(options, () => options.ConnectionString.ServerAddress, false, _loggers);
    }

    [Fact] void should_log_the_token_source_once_at_information_level() => _loggers.Entries.Count(_ => _.Level == LogLevel.Information).ShouldEqual(1);
    [Fact] void should_only_log_the_endpoint_source() => _loggers.Entries.Single(_ => _.Level == LogLevel.Information).Message.ShouldEqual("Requesting client tokens from the authority token endpoint");
    [Fact] void should_not_log_the_host_or_path() => _loggers.Entries.Exists(_ => _.Message.Contains("idp.example.com", StringComparison.Ordinal) || _.Message.Contains("hostile-path-marker", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_log_the_query_string() => _loggers.Entries.Exists(_ => _.Message.Contains("hidden=value", StringComparison.Ordinal)).ShouldBeFalse();
}
