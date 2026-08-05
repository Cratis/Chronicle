// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asking_which_layers_the_harness_substitutes;

/// <summary>
/// A child whose parent key comes from an event property rather than the event source id resolves through the
/// parent hierarchy, which can defer. The harness retries a deferred key once after every other seeded event;
/// a deployed Chronicle defers the partition and redelivers, so seed order here is not arrival order there.
/// </summary>
public class and_a_child_collection_is_keyed_across_streams : Specification
{
    ReadModelScenario<TicketLedger> _scenario;
    IReadOnlyList<ReadModelSubstitution> _substitutions;

    void Establish() => _scenario = new ReadModelScenario<TicketLedger>();

    void Because() => _substitutions = _scenario.Substitutions;

    [Fact] void should_report_the_deferred_key_handling() => _substitutions.Select(_ => _.Layer).ShouldContain(ReadModelSubstitutedLayer.DeferredKeyHandling);
    [Fact] void should_name_the_child_collection() => _substitutions.Single(_ => _.Layer == ReadModelSubstitutedLayer.DeferredKeyHandling).Shape.ShouldContain("tickets");
}
