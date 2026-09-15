// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Patterns;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.given;

public class a_pattern_capture_subscriber : Specification
{
    protected static readonly DateTimeOffset Occurred = new(2026, 8, 24, 9, 15, 0, TimeSpan.Zero);

    protected TestKitSilo _silo;
    protected PatternCaptureSubscriber _subscriber;
    protected IEventFeatureExtractor _extractor;
    protected IPatternMiner _miner;
    protected string _minerIdentity;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;

    async Task Establish()
    {
        _eventStore = "some-store";
        _namespace = EventStoreNamespaceName.Default;

        _extractor = Substitute.For<IEventFeatureExtractor>();
        _miner = Substitute.For<IPatternMiner>();

        _silo = new TestKitSilo();
        _silo.AddService(_extractor);
        _silo.AddService(Substitute.For<ILogger<PatternCaptureSubscriber>>());
        _silo.AddProbe<IPatternMiner>(identity =>
        {
            _minerIdentity = identity.ToString();
            return _miner;
        });

        var key = new ObserverSubscriberKey(
            PatternCapture.ObserverIdentifier,
            _eventStore,
            _namespace,
            EventSequenceId.Log,
            ObserverSubscriberKey.AllPartitions,
            "127.0.0.1:11111@1");
        _subscriber = await _silo.CreateGrainAsync<PatternCaptureSubscriber>(key.ToString());
    }

    protected AppendedEvent EventAt(EventSequenceNumber sequenceNumber, PatternGroupingKey scope)
    {
        var context = EventContext.From(
            _eventStore,
            _namespace,
            new EventType("some-event", EventTypeGeneration.First),
            EventSourceType.Default,
            new EventSourceId(Guid.NewGuid().ToString()),
            EventStreamType.All,
            EventStreamId.Default,
            sequenceNumber,
            CorrelationId.New());

        var @event = new AppendedEvent(context, new ExpandoObject());
        _extractor.Extract(@event).Returns(FeaturesFor(scope));
        return @event;
    }

    protected static EventFeatures FeaturesFor(PatternGroupingKey scope) =>
        new(
            scope,
            "some-command",
            InitiatorType.User,
            scope.Value,
            FacetValue.Unspecified,
            FacetValue.Unspecified,
            FacetValue.Unspecified,
            "some-aggregate",
            2026,
            8,
            DayOfWeek.Monday,
            TimeBucket.Morning,
            Occurred);
}
