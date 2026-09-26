// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.Server.Contracts.for_NamedTagQueryCriterion;

public class when_round_tripping_named_tag_queries : Specification
{
    QueryEventsWithNamedTagsRequest _query;
    SequenceHistogramWithNamedTagsRequest _histogram;

    void Because()
    {
        _query = RoundTrip(new QueryEventsWithNamedTagsRequest
        {
            EventStore = "store",
            EventSequenceId = "log",
            NamedTags = [new NamedTagQueryCriterion { Name = "account", Values = ["one", "two"] }]
        });
        _histogram = RoundTrip(new SequenceHistogramWithNamedTagsRequest
        {
            EventStore = "store",
            EventSequenceId = "log",
            NamedTags = [new NamedTagQueryCriterion { Name = "account", AnyValue = true, Values = [] }]
        });
    }

    [Fact] void should_round_trip_query_values() => _query.NamedTags.Single().Values.ShouldContain("two");
    [Fact] void should_round_trip_query_name() => _query.NamedTags.Single().Name.ShouldEqual("account");
    [Fact] void should_round_trip_histogram_name() => _histogram.NamedTags.Single().Name.ShouldEqual("account");
    [Fact] void should_preserve_a_wildcard_value() => (_histogram.NamedTags.Single().Values?.Any() == true).ShouldBeFalse();
    [Fact] void should_preserve_an_explicit_wildcard() => _histogram.NamedTags.Single().AnyValue.ShouldBeTrue();
    [Fact] void should_not_treat_query_values_as_a_wildcard() => _query.NamedTags.Single().AnyValue.ShouldBeFalse();

    static T RoundTrip<T>(T value)
        where T : class
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, value);
        stream.Position = 0;
        return ProtoBuf.Serializer.Deserialize<T>(stream);
    }
}
