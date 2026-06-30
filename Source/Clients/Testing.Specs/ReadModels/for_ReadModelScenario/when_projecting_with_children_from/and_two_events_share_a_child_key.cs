// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// Verifies child-identity matching: two events carrying the SAME key value resolve to the SAME child
/// (an update), not two duplicate children — the same behavior as the real runtime sink.
/// </summary>
public class and_two_events_share_a_child_key : Specification
{
    ReadModelScenario<LabelLedger> _scenario;
    EventSourceId _ledgerId;

    void Establish()
    {
        _scenario = new ReadModelScenario<LabelLedger>();
        _ledgerId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(
                new LedgerOpened("Operations"),
                new LabelRecorded("alpha", 1m),
                new LabelRecorded("alpha", 9m));

    [Fact] void should_have_a_single_merged_child() => _scenario.Instance!.Labels.Count().ShouldEqual(1);
    [Fact] void should_keep_the_shared_key() => _scenario.Instance!.Labels.Single().Label.ShouldEqual("alpha");
    [Fact] void should_apply_the_latest_value() => _scenario.Instance!.Labels.Single().Amount.ShouldEqual(9m);
}
