// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing.given;

public class a_collection : Specification
{
    protected EventStoreForTesting _store;
    protected EventSourceId _firstId;
    protected ISubject<IEnumerable<ObservedModule>> _observation;
    protected IDisposable _subscription;
    protected List<IEnumerable<ObservedModule>> _emissions;
    protected SemaphoreSlim _signal;
    protected Exception _error;

    async Task Establish()
    {
        _store = new();
        _firstId = EventSourceId.New();
        _emissions = [];
        _signal = new(0);
        await _store.EventLog.Append(_firstId, new ModuleCreated("First"));
    }

    protected void Subscribe() => _subscription = _observation.Subscribe(
        set =>
        {
            _emissions.Add(set);
            _signal.Release();
        },
        error =>
        {
            _error = error;
            _signal.Release();
        });

    protected async Task WaitForEmission()
    {
        (await _signal.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBeTrue();
        _error.ShouldBeNull();
    }

    void Destroy()
    {
        _subscription?.Dispose();
        (_observation as IDisposable)?.Dispose();
        _store.Dispose();
        _signal.Dispose();
    }

    [FromEvent<ModuleCreated>]
    public record ObservedModule(Guid Id, string Name);
}
