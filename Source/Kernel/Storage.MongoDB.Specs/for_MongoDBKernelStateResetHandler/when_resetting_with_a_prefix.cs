// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBKernelStateResetHandler;

public class when_resetting_with_a_prefix : given.a_reset_handler
{
    void Establish() => _storageOptions.DatabaseNamePrefix = "run_";

    Task Because() => _handler.Reset();

    [Fact] void should_drop_only_the_prefixed_event_database() => _client.Received(1).DropDatabaseAsync("run_Testing+es+default", Arg.Any<CancellationToken>());
    [Fact] void should_not_drop_any_other_database() => _client.Received(1).DropDatabaseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_preserve_the_prefixed_cluster_database() => _client.DidNotReceive().DropDatabaseAsync("run_chronicle+main", Arg.Any<CancellationToken>());
    [Fact] void should_not_probe_an_unrelated_database() => _client.DidNotReceive().GetDatabase("other_Testing+es+default", Arg.Any<MongoDatabaseSettings>());
}
