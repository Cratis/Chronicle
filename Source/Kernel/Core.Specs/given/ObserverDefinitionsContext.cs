// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle;

public static class ObserverDefinitionsContext
{
    /// <summary>
    /// Adds storage whose observer definition registry knows no observer, which is the empty definition for every one.
    /// </summary>
    /// <param name="silo">The <see cref="TestKitSilo"/>.</param>
    /// <param name="storage">The <see cref="IStorage"/> to add the registry to; a new one that knows no event stores when not given.</param>
    /// <returns>The <see cref="IObserverDefinitionsStorage"/> to give definitions to.</returns>
    public static IObserverDefinitionsStorage AddObserverDefinitions(this TestKitSilo silo, IStorage? storage = null)
    {
        var definitions = Substitute.For<IObserverDefinitionsStorage>();
        definitions.Get(Arg.Any<ObserverId>()).Returns(ObserverDefinition.Empty);
        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.Observers.Returns(definitions);
        if (storage is null)
        {
            storage = Substitute.For<IStorage>();
            storage.GetEventStores().Returns(_ => Task.FromResult<IEnumerable<EventStoreName>>([]));
        }

        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        silo.AddService(storage);
        return definitions;
    }

    /// <summary>
    /// Adds storage whose observer definition registry knows no observer.
    /// </summary>
    /// <param name="silo">The <see cref="TestKitSilo"/>.</param>
    /// <returns>The same <see cref="TestKitSilo"/>, for continuation.</returns>
    public static TestKitSilo UsingObserverDefinitions(this TestKitSilo silo)
    {
        silo.AddObserverDefinitions();
        return silo;
    }
}
