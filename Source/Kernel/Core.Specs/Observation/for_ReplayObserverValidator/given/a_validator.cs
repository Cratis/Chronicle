// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using FluentValidation;

namespace Cratis.Chronicle.Observation.for_ReplayObserverValidator.given;

public class a_validator : Specification
{
    protected const string EventStore = "some-store";
    protected const string Namespace = "some-namespace";

    /// <summary>
    /// The validator under specification, held as the interface rather than the concrete type - the validator is
    /// internal, and a public specification class cannot expose a field of a less accessible type.
    /// </summary>
    protected IValidator<ReplayObserver> _validator;
    protected IObserverDefinitionsStorage _observerDefinitions;

    void Establish()
    {
        _observerDefinitions = Substitute.For<IObserverDefinitionsStorage>();

        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.Observers.Returns(_observerDefinitions);

        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);

        _validator = new ReplayObserverValidator(storage);
    }

    protected void ObserverIsOwnedBy(string observerId, ObserverOwner owner)
    {
        _observerDefinitions.Has((ObserverId)observerId).Returns(true);
        _observerDefinitions.Get((ObserverId)observerId).Returns(new ObserverDefinition(
            (ObserverId)observerId,
            [],
            EventSequenceId.Log,
            ObserverType.Projection,
            owner,
            false));
    }
}
