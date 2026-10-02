// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBKernelStateResetHandler;

public class when_resetting_without_a_prefix : given.a_reset_handler
{
    Task Because() => _handler.Reset();

    [Fact] void should_keep_the_existing_reset_scope() => _client.Received(5).DropDatabaseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_drop_the_unprefixed_event_database() => _client.Received(1).DropDatabaseAsync("Testing+es+default", Arg.Any<CancellationToken>());
    [Fact] void should_preserve_the_cluster_database() => _client.DidNotReceive().DropDatabaseAsync("chronicle+main", Arg.Any<CancellationToken>());
    [Fact] void should_preserve_mongodb_system_databases() => _client.DidNotReceive().DropDatabaseAsync(Arg.Is<string>(name => name == "admin" || name == "config" || name == "local"), Arg.Any<CancellationToken>());
}
