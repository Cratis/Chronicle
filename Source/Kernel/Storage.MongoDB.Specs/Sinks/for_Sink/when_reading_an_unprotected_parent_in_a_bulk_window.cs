// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_reading_an_unprotected_parent_in_a_bulk_window : given.a_bulk_child_removal_from_all
{
    readonly Key _key = new("parent", ArrayIndexers.NoIndexers);
    readonly ExpandoObject _cached = Document("name", "Ada Lovelace");
    ExpandoObject _result;

    async Task Establish() => await Cache(_key, _cached);

    async Task Because() => _result = await _sink.FindOrDefault(_key);

    [Fact] void should_return_the_cached_fold() => _result.ShouldEqual(_cached);
    [Fact] void should_not_flush_the_bulk_window() => _collection.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(_collection.BulkWriteAsync)).ShouldBeFalse();
    [Fact] void should_not_read_the_stored_document() => _collection.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(_collection.FindAsync)).ShouldBeFalse();
}
