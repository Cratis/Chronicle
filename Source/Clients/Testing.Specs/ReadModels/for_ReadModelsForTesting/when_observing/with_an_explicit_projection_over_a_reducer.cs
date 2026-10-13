// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_an_explicit_projection_over_a_reducer : Specification
{
    EventStoreForTesting _store;
    ISubject<IEnumerable<Tally>> _observation;
    IDisposable _subscription;
    IEnumerable<Tally> _result;
    TaskCompletionSource _received;

    async Task Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterProjection<Tally>(projection => projection
            .From<ModuleCreated>(from => from.Set(row => row.Count).ToValue(42)));
        _store = new(null, Defaults.Instance.ClientArtifactsProvider, options);
        _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("First"));
    }

    async Task Because()
    {
        var decorated = new ReadModelsForTesting(_store.ReadModels, _store.EventLog);
        _observation = decorated.Observe<Tally>();
        _subscription = _observation.Subscribe(
            set =>
            {
                _result = set;
                _received.TrySetResult();
            },
            error => _received.TrySetException(error));
        await _received.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    void Destroy()
    {
        _subscription.Dispose();
        ((IDisposable)_observation).Dispose();
        _store.Dispose();
    }

    [Fact] void should_preserve_the_owning_stores_explicit_projection() => _result.Single().Count.ShouldEqual(42);
}
