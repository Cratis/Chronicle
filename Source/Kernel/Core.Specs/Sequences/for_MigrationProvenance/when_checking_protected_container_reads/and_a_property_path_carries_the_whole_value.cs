// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_MigrationProvenance.when_checking_protected_container_reads;

public class and_a_property_path_carries_the_whole_value : given.a_protected_container
{
    void Establish() => _operations = JsonNode.Parse("""{"renamed":"container"}""")!.AsObject();

    void Because() => _result = MigrationProvenance.CarriesProtectedValuesOpaquely(_operations, _input);

    [Fact] void should_allow_carrying_the_opaque_value() => _result.ShouldBeTrue();
}
