// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_a_token_endpoint_and_logging : given.a_factory_context
{
    RecordingLoggerFactory _loggers;

    void Establish() => _loggers = new RecordingLoggerFactory();

    void Because()
    {
        var options = Options(WithCredentials, new() { TokenEndpoint = "https://idp.example.com/hostile-path-marker/token?hidden=value" });
        _result = TokenProviderFactory.Create(options, () => options.ConnectionString.ServerAddress, false, _loggers);
    }

    [Fact] void should_only_log_the_endpoint_source() => _loggers.Entries.Single(_ => _.Level == LogLevel.Information).Message.ShouldEqual("Requesting client tokens from the configured token endpoint");
    [Fact] void should_not_log_the_endpoint() => _loggers.Entries.Exists(_ => _.Contains("idp.example.com") || _.Contains("hostile-path-marker") || _.Contains("hidden=value")).ShouldBeFalse();
}
