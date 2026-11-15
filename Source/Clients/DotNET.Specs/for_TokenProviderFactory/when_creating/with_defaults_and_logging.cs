// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_defaults_and_logging : given.a_factory_context
{
    RecordingLoggerFactory _loggers;

    void Establish() => _loggers = new RecordingLoggerFactory();

    void Because()
    {
        var options = Options(WithCredentials);
        _result = TokenProviderFactory.Create(options, () => options.ConnectionString.ServerAddress, false, _loggers);
    }

    [Fact] void should_only_log_the_endpoint_source() => _loggers.Entries.Single(_ => _.Level == LogLevel.Information).Message.ShouldEqual("Requesting client tokens from the default server token endpoint");
    [Fact] void should_not_log_the_server_host_or_port() => _loggers.Entries.Exists(_ => _.Contains("localhost") || _.Contains("35000") || _.Contains("connect/token")).ShouldBeFalse();
}
