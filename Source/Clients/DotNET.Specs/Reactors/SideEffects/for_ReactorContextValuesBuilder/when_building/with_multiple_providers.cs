// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorContextValuesBuilder.when_building;

public class with_multiple_providers : Specification
{
    ReactorContextValuesBuilder _builder;
    ReactorContextValues _result;
    EventStreamId _eventStreamId;
    Subject _subject;

    void Establish()
    {
        _eventStreamId = "Yearly";
        _subject = new Subject("my-subject");
        _builder = new ReactorContextValuesBuilder(new KnownInstancesOf<IReactorContextValuesProvider>(
        [
            new EventSourceIdValuesProvider(),
            new EventStreamIdValuesProvider(),
            new SubjectValuesProvider()
        ]));
    }

    void Because() => _result = _builder.Build(new MultiProvidingReactor(_eventStreamId, _subject), EventContext.EmptyWithEventSourceId(EventSourceId.New()));

    [Fact] void should_include_event_source_id() => _result.ContainsKey(WellKnownReactorContextKeys.EventSourceId).ShouldBeTrue();
    [Fact] void should_include_event_stream_id() => ((EventStreamId)_result[WellKnownReactorContextKeys.EventStreamId]).ShouldEqual(_eventStreamId);
    [Fact] void should_include_subject() => ((Subject)_result[WellKnownReactorContextKeys.Subject]).ShouldEqual(_subject);

    class MultiProvidingReactor(EventStreamId eventStreamId, Subject subject) : IReactor, ICanProvideEventStreamId, ICanProvideSubject
    {
        public EventStreamId GetEventStreamId() => eventStreamId;
        public Subject GetSubject() => subject;
    }
}
