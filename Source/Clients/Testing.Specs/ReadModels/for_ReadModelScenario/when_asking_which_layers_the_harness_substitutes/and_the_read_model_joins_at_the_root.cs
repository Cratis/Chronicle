// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asking_which_layers_the_harness_substitutes;

/// <summary>
/// A root-level <c>[Join]</c> is the shape that reached a running system green: the harness corrects the
/// engine's join key against its own sink, so the resolution a deployed Chronicle performs is not the one a
/// spec here exercises. The scenario says so without needing an event.
/// </summary>
public class and_the_read_model_joins_at_the_root : Specification
{
    ReadModelScenario<JoinOrderSummary> _scenario;
    IReadOnlyList<ReadModelSubstitution> _substitutions;

    void Establish() => _scenario = new ReadModelScenario<JoinOrderSummary>();

    void Because() => _substitutions = _scenario.Substitutions;

    [Fact] void should_report_the_join_key_resolution() => _substitutions.Select(_ => _.Layer).ShouldContain(ReadModelSubstitutedLayer.JoinKeyResolution);
    [Fact] void should_name_the_join() => _substitutions.Single(_ => _.Layer == ReadModelSubstitutedLayer.JoinKeyResolution).Shape.ShouldContain("[Join]");
    [Fact] void should_name_the_property_joined_on() => _substitutions.Single(_ => _.Layer == ReadModelSubstitutedLayer.JoinKeyResolution).Shape.ShouldContain("customerId");
    [Fact] void should_say_what_it_costs() => _substitutions.Single(_ => _.Layer == ReadModelSubstitutedLayer.JoinKeyResolution).Consequence.ShouldNotBeEmpty();
}
