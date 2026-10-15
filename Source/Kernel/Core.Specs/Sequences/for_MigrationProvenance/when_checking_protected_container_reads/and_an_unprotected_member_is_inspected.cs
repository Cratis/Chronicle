// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_MigrationProvenance.when_checking_protected_container_reads;

public class and_an_unprotected_member_is_inspected : given.a_protected_container
{
    void Establish()
    {
        _input["public"] = new JsonObject { ["value"] = "not-protected" };
        _operations = JsonNode.Parse("""{"copy":{"$split":{"source":"public.value","separator":"=","part":0}}}""")!.AsObject();
    }

    void Because() => _result = MigrationProvenance.CarriesProtectedValuesOpaquely(_operations, _input);

    [Fact] void should_allow_transforming_the_unprotected_value() => _result.ShouldBeTrue();
}
