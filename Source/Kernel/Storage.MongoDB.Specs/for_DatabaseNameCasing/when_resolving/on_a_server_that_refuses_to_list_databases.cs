// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNameCasing.when_resolving;

/// <summary>
/// Without the right to list databases the name is used as it is requested, as it was before casing was matched.
/// </summary>
public class on_a_server_that_refuses_to_list_databases : Specification
{
    IMongoClient _client;
    ILogger _logger;
    string _resolved;

    void Establish()
    {
        _client = Substitute.For<IMongoClient>();
        _client.ListDatabaseNames(Arg.Any<CancellationToken>()).Returns(_ => throw new InvalidOperationException("not authorized on admin to execute command listDatabases"));
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    void Because() => _resolved = DatabaseNameCasing.Resolve(_client, "Studio+es+cratis", _logger);

    [Fact] void should_resolve_to_the_requested_name() => _resolved.ShouldEqual("Studio+es+cratis");
    [Fact] void should_log_a_warning() => _logger.Received(1).Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<object>(), Arg.Any<InvalidOperationException>(), Arg.Any<Func<object, Exception?, string>>());
}
