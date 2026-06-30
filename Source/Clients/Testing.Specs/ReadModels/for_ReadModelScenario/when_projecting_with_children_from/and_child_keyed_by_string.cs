// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_child_keyed_by_string : Specification
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
                new LabelRecorded("beta", 2m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_label_lines() => _scenario.Instance!.Labels.Count().ShouldEqual(2);
    [Fact] void should_map_first_label_key() => _scenario.Instance!.Labels.First().Label.ShouldEqual("alpha");
    [Fact] void should_map_first_label_amount() => _scenario.Instance!.Labels.First().Amount.ShouldEqual(1m);
    [Fact] void should_map_second_label_key() => _scenario.Instance!.Labels.Last().Label.ShouldEqual("beta");
}
