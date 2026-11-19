// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_FencedGlobalEligibility;

public class when_inspecting_a_definition_holding_only_property_mappings : Specification
{
    string? _reason;

    void Because() => _reason = FencedGlobalEligibility.ReasonFor(given.Definitions.Plain());

    [Fact] void should_have_no_reason_to_rule_it_out() => _reason.ShouldBeNull();
}
