// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target;

/// <summary>
/// An empty event source id against a Guid-backed column. The empty string is not a Guid either, so it takes
/// the same fallback rather than raising the format error the conversion would otherwise throw.
/// </summary>
public class and_the_key_is_empty_for_a_guid_column : given.a_changeset_converter_over_typed_columns
{
    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(RootKey(), JoinOn(nameof(given.JoinTargetReadModel.GuidColumn), string.Empty)));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_fall_back_to_the_unconverted_value() => _result.Value.ShouldEqual(new BsonString(string.Empty));
}
