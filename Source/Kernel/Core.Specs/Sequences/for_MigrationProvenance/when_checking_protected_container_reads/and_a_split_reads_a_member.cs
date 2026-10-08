// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_MigrationProvenance.when_checking_protected_container_reads;

public class and_a_split_reads_a_member : given.a_protected_container
{
    void Establish() => _operations = JsonNode.Parse("""{"public":{"$split":{"source":"container.value","separator":"=","part":0}}}""")!.AsObject();

    void Because() => _result = MigrationProvenance.CarriesProtectedValuesOpaquely(_operations, _input);

    [Fact] void should_reject_inspecting_the_hidden_member() => _result.ShouldBeFalse();
}
