// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_reconciled_capture : an_event_sequence_with_a_capture_observer
{
    protected IReactorDefinitionsStorage _captureDefinitions;
    protected EventType[] _registeredTypes;
    protected IGrainFactory _captureGrainFactory;

    void Establish()
    {
        _registeredTypes = [_eventType];
        _eventTypesStorage.GetLatestForAllEventTypes().Returns(_ => _registeredTypes.Select(type => new EventTypeSchema(type, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema())).ToArray());
        _captureDefinitions = Substitute.For<IReactorDefinitionsStorage>();
        _captureDefinitions.Has(PatternCapture.ObserverIdentifier).Returns(true);
        StoredCaptureTypesAre(_eventType);
        _eventStoreStorage.Reactors.Returns(_captureDefinitions);
        _captureGrainFactory = Substitute.For<IGrainFactory>();
        _captureGrainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_captureObserver);
        var siloDetails = Substitute.For<ILocalSiloDetails>();
        siloDetails.SiloAddress.Returns(SiloAddress.Zero);
        var capture = new PatternCapture(_storage, siloDetails, _captureGrainFactory, NullLogger<PatternCapture>.Instance);
        _patternCapture.Configure().RecoverSubscription(EventStore, EventStoreNamespace)
            .Returns(_ => capture.RecoverSubscription(EventStore, EventStoreNamespace));
    }

    protected void StoredCaptureTypesAre(params EventType[] eventTypes) =>
        _captureDefinitions.Get(PatternCapture.ObserverIdentifier).Returns(new ReactorDefinition(
            PatternCapture.ObserverIdentifier,
            ReactorOwner.Kernel,
            EventSequenceId.Log,
            [.. eventTypes.Select(type => new EventTypeWithKeyExpression(type, Concepts.WellKnownExpressions.EventSourceId))],
            false));
}
