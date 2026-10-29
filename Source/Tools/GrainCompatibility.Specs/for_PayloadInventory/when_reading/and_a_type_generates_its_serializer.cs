// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_type_generates_its_serializer : given.an_inventory
{
    PayloadTypeContract _contract;

    void Because() => _contract = _manifest.Types["FixtureGenerated"];

    [Fact] void should_use_the_orleans_representation() => _contract.Kind.ShouldEqual(PayloadKind.Orleans);
    [Fact] void should_include_the_private_field() => _contract.Members.Single(_ => _.Id == 0).Name.ShouldEqual("_count");
    [Fact] void should_include_the_property() => _contract.Members.Single(_ => _.Id == 1).Name.ShouldEqual("Name");
    [Fact] void should_only_include_annotated_members() => _contract.Members.Count.ShouldEqual(2);
}
