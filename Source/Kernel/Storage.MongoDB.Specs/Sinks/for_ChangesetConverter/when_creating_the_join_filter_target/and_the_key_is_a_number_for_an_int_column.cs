// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target;

/// <summary>
/// A numeric event source id against an int-backed column — the conversion succeeds and the comparand is the
/// number the column actually holds, not the string it arrived as.
/// </summary>
public class and_the_key_is_a_number_for_an_int_column : given.a_changeset_converter_over_typed_columns
{
    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(RootKey(), JoinOn(nameof(given.JoinTargetReadModel.IntColumn), "42")));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_filter_on_the_joined_on_column() => _result.Property.ShouldEqual(nameof(given.JoinTargetReadModel.IntColumn));
    [Fact] void should_compare_against_the_number_the_column_holds() => _result.Value.ShouldEqual(new BsonInt32(42));
}
