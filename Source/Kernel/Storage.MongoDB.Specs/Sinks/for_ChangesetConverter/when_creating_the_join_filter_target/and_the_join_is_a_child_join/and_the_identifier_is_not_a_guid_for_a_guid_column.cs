// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target.and_the_join_is_a_child_join;

/// <summary>
/// The child branch reaches the same conversion with the same failure mode, so it takes the same fallback and
/// the two branches stay symmetric.
/// </summary>
/// <remarks>
/// This bounds the filter only. The array filter the accompanying update needs is built by
/// <see cref="MongoDBConverter.ToMongoDBProperty"/>, which converts the very same identifier eagerly and still
/// raises, so a child join that reaches an unconvertible identifier fails its write regardless. That is not
/// independently reachable through the projection engine — the key resolution pipeline normalizes the array
/// indexer's identifier against the same schema, and raises there first — which is why the fallback stops at
/// the filter rather than being pushed into a converter every property change goes through.
/// </remarks>
public class and_the_identifier_is_not_a_guid_for_a_guid_column : given.a_changeset_converter_over_typed_columns
{
    const string Identifier = "not-a-guid";

    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(
        ChildKey("Children", "ChildId", Identifier),
        JoinOn(nameof(given.JoinTargetReadModel.StringColumn), "ignored-by-the-child-branch")));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_fall_back_to_the_unconverted_value() => _result.Value.ShouldEqual(new BsonString(Identifier));
}
