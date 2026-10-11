// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class and_the_observation_is_disposed : given.a_collection
{
    bool _observingAppends;
    bool _wasObservingAppends;
    int _reads;

    async Task Because()
    {
        var log = Substitute.For<IEventSequence>();
        log.AppendOperations.Returns(Observable.Defer(() =>
        {
            _observingAppends = true;
            return _store.EventLog.AppendOperations.Finally(() => _observingAppends = false);
        }));
        log.GetFromSequenceNumber(EventSequenceNumber.First).Returns(_ =>
        {
            Interlocked.Increment(ref _reads);
            return _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        });
        _observation = new ReadModelsForTesting(_store.ReadModels, log).Observe<ObservedModule>();
        Subscribe();
        await WaitForEmission();
        _wasObservingAppends = _observingAppends;
        ((IDisposable)_observation).Dispose();
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("Second"));
        _observation.OnNext([new ObservedModule(Guid.NewGuid(), "After disposal")]);
    }

    [Fact] void should_have_listened_to_appends_before_disposal() => _wasObservingAppends.ShouldBeTrue();
    [Fact] void should_detach_from_appends() => _observingAppends.ShouldBeFalse();
    [Fact] void should_not_start_another_rebuild() => _reads.ShouldEqual(1);
    [Fact] void should_not_emit_again() => _emissions.Count.ShouldEqual(1);
}
