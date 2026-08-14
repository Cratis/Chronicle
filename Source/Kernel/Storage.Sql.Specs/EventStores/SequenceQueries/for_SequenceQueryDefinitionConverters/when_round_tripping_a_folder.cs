// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries;

namespace Cratis.Chronicle.Storage.Sql.EventStores.for_SequenceQueryDefinitionConverters;

/// <summary>
/// The SQL backend has to keep a folder exactly where MongoDB does - a converter that drops its path
/// or its scope would scatter the hierarchy on the next read.
/// </summary>
public class when_round_tripping_a_folder : Specification
{
    const string Path = "Diagnostics/Failures";

    SequenceQueryFolderDefinition _original;
    SequenceQueryFolderDefinition _result;

    void Establish() => _original = new(
        "b8f1c0de-0000-4000-8000-000000000002",
        SequenceQueryScope.Everyone,
        "alice",
        "default",
        Path);

    void Because() => _result = _original.ToSql().ToKernel();

    [Fact] void should_keep_the_identity() => _result.Id.ShouldEqual(_original.Id);
    [Fact] void should_keep_the_path() => _result.Path.Value.ShouldEqual(Path);
    [Fact] void should_keep_the_scope() => _result.Scope.ShouldEqual(_original.Scope);
    [Fact] void should_keep_the_owner() => _result.Owner.ShouldEqual(_original.Owner);
    [Fact] void should_keep_the_namespace() => _result.Namespace.ShouldEqual(_original.Namespace);
}
