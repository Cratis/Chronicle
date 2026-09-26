// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences.for_SequenceHistogramBucket;

public class when_histogramming_with_missing_named_tags : Specification
{
    Exception _exception;
    IStorage _storage;

    void Establish() => _storage = Substitute.For<IStorage>();

    async Task Because() => _exception = await Catch.Exception(async () => await SequenceHistogramBucket.SequenceHistogramWithNamedTags(
        _storage,
        "store",
        "namespace",
        "log",
        []));

    [Fact] void should_refuse_the_query() => _exception.ShouldBeOfExactType<Storage.EventSequences.InvalidNamedTagCriterion>();
    [Fact] void should_not_read_storage() => _storage.DidNotReceiveWithAnyArgs().GetEventStore("store");
}
