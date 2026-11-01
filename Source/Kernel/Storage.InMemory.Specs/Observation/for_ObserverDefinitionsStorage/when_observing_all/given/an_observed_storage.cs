// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage.when_observing_all.given;

public class an_observed_storage : Specification
{
    protected ObserverDefinitionsStorage _storage;
    protected List<ObserverDefinition[]> _emissions = [];
    IDisposable _subscription;

    void Establish()
    {
        _storage = new();
        _subscription = _storage.ObserveAll().Subscribe(definitions => _emissions.Add(definitions.ToArray()));
    }

    protected static ObserverDefinition Definition() => new("observer", [], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true);

    void Destroy()
    {
        _subscription.Dispose();
        _storage.Dispose();
    }
}
