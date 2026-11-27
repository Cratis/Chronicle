// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverLag.when_getting;

public class and_the_requested_sequence_does_not_own_the_observer : given.an_observer_lag_query
{
    const string WrongSequence = "another-sequence";
    ObserverLag _result;

    void Establish()
    {
        _grainFactory.GetGrain<IObserver>(new ObserverKey(Id, Store, Namespace, WrongSequence)).Returns(_grain);
        _grain.GetInformation().Returns(new ObserverInformationSnapshot(Definition, new ObserverState(), true, null));
    }

    async Task Because() => _result = await Query(WrongSequence);

    [Fact] void should_report_unknown_state() => _result.Cause.ShouldEqual(ObserverLagCause.UnknownState);
    [Fact] async Task should_not_check_the_directory() => await _directory.DidNotReceive().GetActivationAddress(Arg.Any<IAddressable>());
    [Fact] async Task should_not_read_the_observer() => await _grain.DidNotReceive().GetInformation();
}
