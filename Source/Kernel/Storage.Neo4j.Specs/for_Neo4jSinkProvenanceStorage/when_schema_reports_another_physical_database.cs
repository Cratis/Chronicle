// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Chronicle.Storage.Neo4j.Neo4jSinkProvenanceStatements;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jSinkProvenanceStorage;

public class when_schema_reports_another_physical_database : given.a_graph_database
{
    Exception? _failure;

    void Establish() => SetIdentity(_schema, "another-identity");

    async Task Because() => _failure = await Catch.Exception(_provenance.ReadState);

    [Fact] void should_deny_admission() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_not_provision_a_constraint_in_the_other_database() => await _schema.DidNotReceive().RunAsync(CreateConstraint, Arg.Any<object>());
    [Fact] async Task should_not_seed_runtime_provenance() => await _runtime.DidNotReceive().RunAsync(Seed, Arg.Any<object>());
}
