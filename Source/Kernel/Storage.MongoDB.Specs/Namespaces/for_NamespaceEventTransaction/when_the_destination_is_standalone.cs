// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_destination_is_standalone : given.a_namespace_transaction
{
    void Establish() => _destination.RunCommandAsync(Arg.Any<Command<BsonDocument>>(), Arg.Any<ReadPreference>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(new BsonDocument("isWritablePrimary", true)));

    Task Because() => ExecuteCatching();

    [Fact] void should_report_unsupported_before_writing() => _failure.ShouldBeOfExactType<NamespaceCommitNotSupported>();
    [Fact] void should_not_start_a_session() => _attempts.ShouldEqual(0);
    [Fact] void should_not_touch_the_marker_or_events() => _steps.ShouldBeEmpty();
}
