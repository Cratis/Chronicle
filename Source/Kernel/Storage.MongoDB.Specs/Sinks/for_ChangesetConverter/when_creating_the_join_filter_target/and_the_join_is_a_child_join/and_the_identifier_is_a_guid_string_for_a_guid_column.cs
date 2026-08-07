// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target.and_the_join_is_a_child_join;

/// <summary>
/// The child branch matches every root document holding the child, so it filters on the array indexer's
/// identifier rather than on the join key. It converted through the schema before the root branch did, and
/// this pins that it still does — the two branches drifting apart is what produced the root-level defect.
/// </summary>
public class and_the_identifier_is_a_guid_string_for_a_guid_column : given.a_changeset_converter_over_typed_columns
{
    readonly Guid _identifier = Guid.NewGuid();

    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(
        ChildKey("Children", "ChildId", _identifier.ToString()),
        JoinOn(nameof(given.JoinTargetReadModel.StringColumn), "ignored-by-the-child-branch")));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_filter_on_the_child_identifier_path() => _result.Property.ShouldEqual("Children.ChildId");
    [Fact] void should_compare_against_the_binary_uuid_the_column_holds() => _result.Value.ShouldEqual(new BsonBinaryData(_identifier, GuidRepresentation.Standard));
}
