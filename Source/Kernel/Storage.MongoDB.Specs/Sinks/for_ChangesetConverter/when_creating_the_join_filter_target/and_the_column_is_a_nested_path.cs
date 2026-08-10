// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target;

/// <summary>
/// A join declared on a nested column. The schema lookup has to descend the path for the conversion to find
/// the format at all — a lookup that stopped at the first segment would silently produce the raw fallback.
/// </summary>
public class and_the_column_is_a_nested_path : given.a_changeset_converter_over_typed_columns
{
    readonly Guid _key = Guid.NewGuid();

    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(RootKey(), JoinOn("Nested.NestedGuidColumn", _key.ToString())));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_filter_on_the_nested_path() => _result.Property.ShouldEqual("Nested.NestedGuidColumn");
    [Fact] void should_compare_against_the_binary_uuid_the_column_holds() => _result.Value.ShouldEqual(new BsonBinaryData(_key, GuidRepresentation.Standard));
}
