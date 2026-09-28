// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences.for_AppendedEvent;

public class when_querying_with_missing_named_tags : Specification
{
    Exception _exception;
    IStorage _storage;

    void Establish() => _storage = Substitute.For<IStorage>();

    async Task Because() => _exception = await Catch.Exception(async () => await AppendedEvent.QueryEventsWithNamedTags(
        _storage,
        Substitute.For<IEventCompliance>(),
        new JsonSerializerOptions(),
        Substitute.For<IQueryContextManager>(),
        "store",
        "namespace",
        "log",
        []));

    [Fact] void should_refuse_the_query() => _exception.ShouldBeOfExactType<Storage.EventSequences.InvalidNamedTagCriterion>();
    [Fact] void should_not_read_storage() => _storage.DidNotReceiveWithAnyArgs().GetEventStore("store");
}
