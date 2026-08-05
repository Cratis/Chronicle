// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelJsonSerialization;

/// <summary>
/// Only a non-nullable declaration is a promise worth keeping. A read model that genuinely needs to tell "no
/// collection" from "an empty one" says so by declaring the property nullable, and keeps the distinction - the
/// same carve-out the TypeScript reader made when it was fixed for this in Fundamentals 7.16.8. Present values,
/// including an explicit empty one, are never touched.
/// </summary>
public class when_the_declaration_keeps_the_distinction : given.read_model_serializer_options
{
    record Order(string Id, IReadOnlyList<string>? Lines, IReadOnlyList<string> Tags, string? Reference);

    Order _absent;
    Order _explicitlyNull;
    Order _present;

    void Because()
    {
        _absent = Read<Order>("""{"id":"the-order"}""");
        _explicitlyNull = Read<Order>("""{"id":"the-order","lines":null}""");
        _present = Read<Order>("""{"id":"the-order","lines":["a"],"tags":[]}""");
    }

    [Fact] void should_leave_an_absent_nullable_collection_null() => _absent.Lines.ShouldBeNull();
    [Fact] void should_leave_an_explicitly_null_nullable_collection_null() => _explicitlyNull.Lines.ShouldBeNull();
    [Fact] void should_still_fill_the_non_nullable_collection_beside_it() => _absent.Tags.ShouldBeEmpty();
    [Fact] void should_leave_a_present_collection_alone() => _present.Lines.ShouldContainOnly(["a"]);
    [Fact] void should_leave_an_explicitly_empty_collection_alone() => _present.Tags.ShouldBeEmpty();
    [Fact] void should_leave_an_absent_nullable_scalar_null() => _absent.Reference.ShouldBeNull();
}
