// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target;

/// <summary>
/// A join that resolved no key at all. The comparand is null, which the write path treats as "do not write":
/// an equality filter against null matches every document whose column is null OR absent, so an UpdateMany
/// built from it stamps the whole collection.
/// </summary>
public class and_the_key_is_null : given.a_changeset_converter_over_typed_columns
{
    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(RootKey(), JoinOn(nameof(given.JoinTargetReadModel.GuidColumn), null)));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_have_no_value_to_compare_against() => _result.Value.ShouldEqual(BsonNull.Value);
}
