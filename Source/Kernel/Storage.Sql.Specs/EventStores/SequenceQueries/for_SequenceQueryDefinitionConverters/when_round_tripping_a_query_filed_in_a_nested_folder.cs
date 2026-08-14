// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries;

namespace Cratis.Chronicle.Storage.Sql.EventStores.for_SequenceQueryDefinitionConverters;

/// <summary>
/// The SQL backend has to file a saved query exactly where MongoDB does — a converter that drops the
/// folder here would scatter every query back to the root on the next read.
/// </summary>
public class when_round_tripping_a_query_filed_in_a_nested_folder : Specification
{
    const string Folder = "Diagnostics/Failures";

    Concepts.SequenceQueries.SequenceQueryDefinition _original;
    Concepts.SequenceQueries.SequenceQueryDefinition _result;

    void Establish() => _original = new(
        "b8f1c0de-0000-4000-8000-000000000001",
        "Failed appends",
        SequenceQueryScope.Everyone,
        "alice",
        Folder,
        "default",
        "event-log",
        Concepts.SequenceQueries.SequenceQueryFilter.Empty,
        SequenceQuerySortBy.Occurred,
        Descending: true);

    void Because() => _result = _original.ToSql().ToKernel();

    [Fact] void should_keep_the_folder() => _result.Folder.Value.ShouldEqual(Folder);
    [Fact] void should_keep_the_name() => _result.Name.ShouldEqual(_original.Name);
    [Fact] void should_keep_the_scope() => _result.Scope.ShouldEqual(_original.Scope);
    [Fact] void should_keep_the_owner() => _result.Owner.ShouldEqual(_original.Owner);
    [Fact] void should_keep_what_it_is_ordered_by() => _result.SortBy.ShouldEqual(_original.SortBy);
}
