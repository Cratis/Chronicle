// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_writing_a_page;

/// <summary>
/// An unsatisfied write concern says nothing about which writes were applied, so every partition of the page is unknown.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> supplying the database.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_write_concern_fails(MongoDBFixture fixture) : given.a_page_whose_bulk_write_throws(fixture)
{
    protected override Exception Failure { get; } = new MongoWriteConcernException(
        new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))),
        "The write concern was not satisfied",
        new WriteConcernResult(new BsonDocument("ok", 1)));

    async Task Because() => _failures = [.. await WriteThePage()];

    [Fact] void should_report_every_partition_of_the_page() => _failures.Select(_ => _.EventSourceId.Value).ShouldContainOnly("first", "second", "third");
    [Fact] void should_report_every_partition_with_an_unknown_outcome() => _failures.ShouldEachConformTo(_ => _.OutcomeUnknown);
    [Fact] void should_report_each_partition_from_its_event() => FailureFor("second").EventSequenceNumber.Value.ShouldEqual(2UL);
    [Fact] void should_name_the_error_in_the_reason() => _failures.ShouldEachConformTo(_ => _.Reason.Contains(Failure.GetType().Name));
}
