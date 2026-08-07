// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target;

/// <summary>
/// A join declared on a column the read model schema does not carry — a misdeclared join, or one written
/// against a generation the schema has moved past. There is no format to convert through, so the value is
/// used as it arrived and the filter matches nothing.
/// </summary>
public class and_the_column_is_missing_from_the_schema : given.a_changeset_converter_over_typed_columns
{
    const string Key = "some-key";

    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(RootKey(), JoinOn("ColumnThatDoesNotExist", Key)));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_filter_on_the_declared_column() => _result.Property.ShouldEqual("ColumnThatDoesNotExist");
    [Fact] void should_use_the_raw_value() => _result.Value.ShouldEqual(new BsonString(Key));
}
