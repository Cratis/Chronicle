// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_resolving_a_service_twice : given.testing_services
{
    IObservers _firstObservers;
    IObservers _secondObservers;
    IReactors _firstReactors;
    IReactors _secondReactors;
    Contracts.Sequences.IEventSequences _firstSequences;
    Contracts.Sequences.IEventSequences _secondSequences;
    Contracts.Occurrences.IOccurrences _firstOccurrences;
    Contracts.Occurrences.IOccurrences _secondOccurrences;
    IReadModels _firstReadModels;
    IReadModels _secondReadModels;
    ICompliance _firstCompliance;
    ICompliance _secondCompliance;
    IEventTypes _firstEventTypes;
    IEventTypes _secondEventTypes;

    void Because()
    {
        _firstObservers = _services.Observers;
        _secondObservers = _services.Observers;
        _firstReactors = _services.Reactors;
        _secondReactors = _services.Reactors;
        _firstSequences = _services.Sequences;
        _secondSequences = _services.Sequences;
        _firstOccurrences = _services.Occurrences;
        _secondOccurrences = _services.Occurrences;
        _firstReadModels = _services.ReadModels;
        _secondReadModels = _services.ReadModels;
        _firstCompliance = _services.Compliance;
        _secondCompliance = _services.Compliance;
        _firstEventTypes = _services.EventTypes;
        _secondEventTypes = _services.EventTypes;
    }

    [Fact] void should_return_the_same_observers() => _firstObservers.ShouldBeSame(_secondObservers);
    [Fact] void should_return_the_same_reactors() => _firstReactors.ShouldBeSame(_secondReactors);
    [Fact] void should_return_the_same_sequences() => _firstSequences.ShouldBeSame(_secondSequences);
    [Fact] void should_return_the_same_occurrences() => _firstOccurrences.ShouldBeSame(_secondOccurrences);
    [Fact] void should_return_the_same_read_models() => _firstReadModels.ShouldBeSame(_secondReadModels);
    [Fact] void should_return_the_same_compliance() => _firstCompliance.ShouldBeSame(_secondCompliance);
    [Fact] void should_return_the_same_event_types() => _firstEventTypes.ShouldBeSame(_secondEventTypes);
}
