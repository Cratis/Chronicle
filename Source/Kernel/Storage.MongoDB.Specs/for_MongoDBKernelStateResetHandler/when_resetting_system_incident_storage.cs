// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBKernelStateResetHandler;

public class when_resetting_system_incident_storage : given.a_reset_handler
{
    void Establish()
    {
        var cursor = Substitute.For<IAsyncCursor<string>>();
        cursor.Current.Returns(["chronicle+main", "System+es+Default"]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        _client.ListDatabaseNamesAsync(Arg.Any<CancellationToken>()).Returns(cursor);
    }

    Task Because() => _handler.Reset();

    [Fact] void should_drop_the_namespace_containing_history_and_incidents() => _client.Received(1).DropDatabaseAsync("System+es+Default", Arg.Any<CancellationToken>());
    [Fact] void should_keep_the_cluster_database() => _client.DidNotReceive().DropDatabaseAsync("chronicle+main", Arg.Any<CancellationToken>());
}
