// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target;

/// <summary>
/// A join source whose event source id is not a Guid, against a Guid-backed column. The conversion cannot
/// succeed, and the documented fallback is the unconverted value — a filter that matches nothing. Throwing
/// here would fail the sink write and freeze the partition permanently, which is strictly worse than the
/// silent no-match this replaced.
/// </summary>
public class and_the_key_is_not_a_guid_for_a_guid_column : given.a_changeset_converter_over_typed_columns
{
    const string Key = "not-a-guid";

    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(RootKey(), JoinOn(nameof(given.JoinTargetReadModel.GuidColumn), Key)));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_filter_on_the_joined_on_column() => _result.Property.ShouldEqual(nameof(given.JoinTargetReadModel.GuidColumn));
    [Fact] void should_fall_back_to_the_unconverted_value() => _result.Value.ShouldEqual(new BsonString(Key));
}
