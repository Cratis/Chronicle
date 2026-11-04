// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences.for_MigrationProvenance.when_checking_protected_container_reads;

public class and_a_split_reads_a_protected_collection_element : given.a_protected_container
{
    void Establish()
    {
        _input = new JsonObject { ["items"] = new JsonArray(new JsonObject { ["secret"] = $"{VerificationMarkers.NewPrefix()}0" }) };
        _operations = JsonNode.Parse("""{"items[].derived":{"$split":{"source":"items[].secret","separator":"=","part":0}}}""")!.AsObject();
    }

    void Because() => _result = MigrationProvenance.CarriesProtectedValuesOpaquely(_operations, _input);

    [Fact] void should_reject_inspecting_the_hidden_element_value() => _result.ShouldBeFalse();
}
