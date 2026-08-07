// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target;

/// <summary>
/// A non-numeric event source id against an int-backed column. Guid is the common case of a column a raw
/// event source id cannot be coerced to, but it is not the only one — every format takes the same fallback.
/// </summary>
public class and_the_key_is_not_a_number_for_an_int_column : given.a_changeset_converter_over_typed_columns
{
    const string Key = "not-a-number";

    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(RootKey(), JoinOn(nameof(given.JoinTargetReadModel.IntColumn), Key)));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_fall_back_to_the_unconverted_value() => _result.Value.ShouldEqual(new BsonString(Key));
}
