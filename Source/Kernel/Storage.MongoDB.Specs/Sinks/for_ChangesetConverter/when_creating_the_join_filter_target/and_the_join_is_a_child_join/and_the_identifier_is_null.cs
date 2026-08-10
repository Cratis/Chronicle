// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target.and_the_join_is_a_child_join;

/// <summary>
/// A child join with no identifier to locate the child by. The comparand is null, and the write path refuses
/// it for the same reason as the root branch: every root holding a child with a null or absent identifier
/// would match, and the update would be applied to all of them.
/// </summary>
public class and_the_identifier_is_null : given.a_changeset_converter_over_typed_columns
{
    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(
        ChildKey("Children", "ChildId", null),
        JoinOn(nameof(given.JoinTargetReadModel.StringColumn), "ignored-by-the-child-branch")));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_have_no_value_to_compare_against() => _result.Value.ShouldEqual(BsonNull.Value);
}
