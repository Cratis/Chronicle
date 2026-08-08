// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target;

/// <summary>
/// A composite key reaching the join filter. It cannot match a scalar column, but it must not throw on the
/// way to not matching: a composite-keyed read model that also declares a join would otherwise fail its
/// partition rather than simply write nothing.
/// </summary>
public class and_the_key_is_a_composite_object : given.a_changeset_converter_over_typed_columns
{
    Exception _error;
    JoinFilterTarget _result;

    void Because()
    {
        dynamic key = new ExpandoObject();
        key.first = "one";
        key.second = "two";
        _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(RootKey(), JoinOn(nameof(given.JoinTargetReadModel.StringColumn), key)));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_compare_against_a_document_that_matches_no_scalar_column() => _result.Value.BsonType.ShouldEqual(BsonType.Document);
}
