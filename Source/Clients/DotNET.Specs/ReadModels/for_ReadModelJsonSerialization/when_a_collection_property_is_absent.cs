// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelJsonSerialization;

/// <summary>
/// The read-model sink omits an empty child collection rather than writing <c>[]</c> - deliberately, so a
/// parallel replay cannot overwrite children a sibling event already wrote. A read model nonetheless declares
/// that collection non-nullable, so nullable-reference analysis never warns and nothing prompts a guard, and the
/// property came back <see langword="null"/> anyway - on the state every one of these read models is in the
/// moment it is created. That is not an edge case; it is the first read of every new instance.
/// </summary>
public class when_a_collection_property_is_absent : given.read_model_serializer_options
{
    record Order(string Id, IReadOnlyList<string> Lines, IEnumerable<string> Tags, string[] Notes, HashSet<string> Labels);

    Order _result;

    void Because() => _result = Read<Order>("""{"id":"the-order"}""");

    [Fact] void should_materialize_a_read_only_list_as_empty() => _result.Lines.ShouldBeEmpty();
    [Fact] void should_materialize_an_enumerable_as_empty() => _result.Tags.ShouldBeEmpty();
    [Fact] void should_materialize_an_array_as_empty() => _result.Notes.ShouldBeEmpty();
    [Fact] void should_materialize_a_set_as_empty() => _result.Labels.ShouldBeEmpty();
    [Fact] void should_leave_a_present_scalar_alone() => _result.Id.ShouldEqual("the-order");
}
