// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_method_takes_a_record : given.an_inventory
{
    PayloadTypeContract _contract;

    void Because() => _contract = Contract<Payload>();

    [Fact] void should_unwrap_the_task_result() => _contract.Kind.ShouldEqual(PayloadKind.Json);
    [Fact] void should_use_camel_case_names() => _contract.Members.Select(_ => _.Name).ShouldContain("displayName");
    [Fact] void should_honor_the_explicit_name() => _contract.Members.Select(_ => _.Name).ShouldContain("renamed");
    [Fact] void should_exclude_ignored_properties() => _contract.Members.Any(_ => _.Name == "ignored").ShouldBeFalse();
    [Fact] void should_keep_conditionally_ignored_properties() => _contract.Members.Select(_ => _.Name).ShouldContain("conditional");
}
