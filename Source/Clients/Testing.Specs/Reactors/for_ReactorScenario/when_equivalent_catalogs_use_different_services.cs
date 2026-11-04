// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_equivalent_catalogs_use_different_services : Specification
{
    Defaults _defaults;
    ReactorScenario<RecordingReactor> _first;
    ReactorScenario<RecordingReactor> _second;
    ReadModelScenario<BonusTally> _firstModel;
    ReadModelScenario<BonusTally> _secondModel;
    readonly Probe _firstProbe = new(10);
    readonly Probe _secondProbe = new(100);

    void Establish()
    {
        _defaults = new(new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(Tallied)],
            Reducers = [typeof(BonusTallyReducer)],
            Reactors = [typeof(RecordingReactor)]
        });
        _first = CreateScenario(_firstProbe);
        _second = CreateScenario(_secondProbe);
        _firstModel = CreateReadModelScenario(_firstProbe);
        _secondModel = CreateReadModelScenario(_secondProbe);
    }

    async Task Because()
    {
        await _firstModel.Given.ForEventSource("shared-source").Events(new Tallied());
        await _secondModel.Given.ForEventSource("shared-source").Events(new Tallied());
        _first.Given.ForEventSourceId("shared-source").ReadModel(_firstModel.Instance!);
        _second.Given.ForEventSourceId("shared-source").ReadModel(_secondModel.Instance!);
        await _first.Given.ForEventSource("shared-source").Events(new Tallied());
        await _second.Given.ForEventSource("shared-source").Events(new Tallied());
    }

    [Fact] void should_activate_the_first_reducer_with_its_own_services() => _firstProbe.ObservedCount.ShouldEqual(11);
    [Fact] void should_activate_the_second_reducer_with_its_own_services() => _secondProbe.ObservedCount.ShouldEqual(101);
    [Fact] void should_not_share_reactor_or_model_instances() => _firstProbe.Model.ShouldNotBeSame(_secondProbe.Model);

    ReactorScenario<RecordingReactor> CreateScenario(Probe probe)
    {
        var scenario = new ReactorScenario<RecordingReactor>(_defaults);
        scenario.Services.AddSingleton<IBonusProvider>(probe);
        scenario.Services.AddSingleton(probe);
        return scenario;
    }

    ReadModelScenario<BonusTally> CreateReadModelScenario(Probe probe)
    {
        var scenario = new ReadModelScenario<BonusTally>(null, _defaults);
        scenario.Services.AddSingleton<IBonusProvider>(probe);
        return scenario;
    }

    void Destroy()
    {
        _firstModel?.Dispose();
        _secondModel?.Dispose();
        _first?.Dispose();
        _second?.Dispose();
        (_defaults?.EventStore as IDisposable)?.Dispose();
    }

    public class Probe(int bonus) : IBonusProvider
    {
        public int ObservedCount { get; set; }
        public BonusTally? Model { get; set; }
        public int GetBonus() => bonus;
    }

    public class RecordingReactor(Probe probe) : IReactor
    {
        public void Handle(Tallied @event, BonusTally model)
        {
            probe.ObservedCount = model.Count;
            probe.Model = model;
        }
    }
}
