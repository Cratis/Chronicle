// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target;

/// <summary>
/// The string-backed column that always worked — a control. It must keep producing the same comparand it did
/// before the conversion was introduced, because a string join key against a string column needs no coercion.
/// </summary>
public class and_the_key_is_a_string_for_a_string_column : given.a_changeset_converter_over_typed_columns
{
    const string Key = "918273645";

    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(RootKey(), JoinOn(nameof(given.JoinTargetReadModel.StringColumn), Key)));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_filter_on_the_joined_on_column() => _result.Property.ShouldEqual(nameof(given.JoinTargetReadModel.StringColumn));
    [Fact] void should_compare_against_the_string_the_column_holds() => _result.Value.ShouldEqual(new BsonString(Key));
}
