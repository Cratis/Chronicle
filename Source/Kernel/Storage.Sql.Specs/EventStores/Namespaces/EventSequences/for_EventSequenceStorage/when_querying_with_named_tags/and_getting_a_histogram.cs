// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_getting_a_histogram : given.an_event_sequence_storage
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () => await _storage.GetHistogram(HistogramResolution.Day, new() { NamedTags = [new(new TagName("account"))] }));

    [Fact] void should_reject_the_query() => _error.ShouldBeOfExactType<NamedTagsNotSupported>();
}
