// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf;

namespace Cratis.Chronicle.Sequences.for_AppendedEventConverters;

public class when_serializing_erased_paths : Specification
{
    AppendedEventResponse _result;
    AppendedEventResponse _olderResponse;
    IReadOnlyList<(string Name, int Tag)> _members;

    void Because()
    {
        var response = new AppendedEventResponse
        {
            Id = "42",
            Content = "{\"count\":0,\"flag\":false}",
            OriginalContent = "original",
            RevisedGeneration = 2,
            GenerationalContent = new Dictionary<int, string> { [1] = "generation" },
            ErasedProperties = ["count", "contacts[0].email", "flag"]
        };
        _result = Serializer.DeepClone(response);
        _olderResponse = Serializer.DeepClone(new AppendedEventResponse { Id = "43", Content = "{}" });
        _members = typeof(AppendedEventResponse).GetProperties()
            .Select(property => (property.Name, Tag: property.GetCustomAttributes(typeof(ProtoMemberAttribute), false).Cast<ProtoMemberAttribute>().SingleOrDefault()?.Tag ?? 0))
            .Where(_ => _.Tag != 0).OrderBy(_ => _.Tag).ToArray();
    }

    [Fact] void should_round_trip_multiple_paths() => _result.ErasedProperties.ShouldEqual<IEnumerable<string>>(["count", "contacts[0].email", "flag"]);
    [Fact] void should_default_older_responses_to_empty_metadata() => _olderResponse.ErasedProperties.ShouldBeEmpty();
    [Fact] void should_preserve_original_content() => _result.OriginalContent.ShouldEqual("original");
    [Fact] void should_preserve_current_content() => _result.Content.ShouldEqual("{\"count\":0,\"flag\":false}");
    [Fact] void should_preserve_revised_generation() => _result.RevisedGeneration.ShouldEqual((uint?)2);
    [Fact] void should_preserve_generational_content() => _result.GenerationalContent.Single(_ => _.Key == 1).Value.ShouldEqual("generation");
    [Fact]
    void should_append_a_tag_without_renumbering_existing_members() => _members.ShouldEqual<IEnumerable<(string Name, int Tag)>>([
        ("Id", 1), ("Context", 2), ("Content", 3), ("OriginalContent", 4), ("Revisions", 5), ("GenerationalContent", 6), ("RevisedGeneration", 7), ("ErasedProperties", 8)]);
}
