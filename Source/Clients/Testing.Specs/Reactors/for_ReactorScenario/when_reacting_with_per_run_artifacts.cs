// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.Testing.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_reacting_with_per_run_artifacts : Specification
{
    IClientArtifactsProvider _firstArtifacts;
    IClientArtifactsProvider _secondArtifacts;
    Defaults _firstDefaults;
    Defaults _secondDefaults;
    RecordingHandlers _firstHandlers;
    RecordingHandlers _secondHandlers;
    ReactorScenario<ReservationReactor> _first;
    ReactorScenario<ReservationReactor> _second;

    void Establish()
    {
        _firstArtifacts = Substitute.For<IClientArtifactsProvider>();
        _firstArtifacts.EventTypes.Returns([typeof(ReservationMade), typeof(MemberActivityRecorded)]);
        _secondArtifacts = Substitute.For<IClientArtifactsProvider>();
        _secondArtifacts.EventTypes.Returns([typeof(ReservationMade), typeof(MemberActivityRecorded)]);
        _firstDefaults = new Defaults(_firstArtifacts);
        _secondDefaults = new Defaults(_secondArtifacts);
        _firstHandlers = new RecordingHandlers();
        _secondHandlers = new RecordingHandlers();
        _first = new ReactorScenario<ReservationReactor>(_firstDefaults, sideEffectHandlers: _firstHandlers);
        _second = new ReactorScenario<ReservationReactor>(_secondDefaults, sideEffectHandlers: _secondHandlers);
    }

    async Task Because()
    {
        await _first.Given.ForEventSource(EventSourceId.New()).Events(new ReservationMade("first"));
        await _second.Given.ForEventSource(EventSourceId.New()).Events(new ReservationMade("second"));
    }

    [Fact] void should_use_the_first_runs_artifacts_in_its_event_store() => ((EventStoreForTesting)_firstHandlers.EventStore!).ClientArtifactsProvider.ShouldBeSame(_firstArtifacts);
    [Fact] void should_use_the_second_runs_artifacts_in_its_event_store() => ((EventStoreForTesting)_secondHandlers.EventStore!).ClientArtifactsProvider.ShouldBeSame(_secondArtifacts);
    [Fact] void should_use_the_first_runs_event_types() => _firstHandlers.EventStore!.EventTypes.AllClrTypes.ShouldContainOnly(_firstDefaults.EventTypes.AllClrTypes);
    [Fact] void should_use_the_second_runs_event_types() => _secondHandlers.EventStore!.EventTypes.AllClrTypes.ShouldContainOnly(_secondDefaults.EventTypes.AllClrTypes);

    sealed class RecordingHandlers : IReactorSideEffectHandlers
    {
        internal IEventStore? EventStore { get; private set; }

        public bool CanHandle(ReactorContext reactorContext, object value) => true;

        public Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
        {
            EventStore = eventStore;
            return Task.FromResult(Result.Success<ReactorSideEffectFailure>());
        }
    }
}
