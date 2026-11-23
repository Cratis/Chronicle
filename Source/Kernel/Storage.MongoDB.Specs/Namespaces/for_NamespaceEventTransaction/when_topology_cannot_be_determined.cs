// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_topology_cannot_be_determined : given.a_namespace_transaction
{
    readonly TimeoutException _topologyFailure = new();

    void Establish() => _destination.RunCommandAsync(Arg.Any<Command<BsonDocument>>(), Arg.Any<ReadPreference>(), Arg.Any<CancellationToken>())
        .Returns<BsonDocument>(_ => throw _topologyFailure);

    Task Because() => ExecuteCatching();

    [Fact] void should_preserve_the_unknown_topology_failure() => _failure.ShouldEqual(_topologyFailure);
    [Fact] void should_not_treat_unknown_topology_as_transaction_support() => _attempts.ShouldEqual(0);
    [Fact] void should_not_write_any_data() => _steps.ShouldBeEmpty();
}
