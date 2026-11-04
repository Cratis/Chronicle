// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;

namespace Cratis.Chronicle.Events.for_AppendedEventConverters;

public class when_converting_sequence_erased_properties : Specification
{
    IEventTypes _eventTypes;
    Contracts.Sequences.AppendedEventResponse _response;
    List<string> _paths;
    AppendedEvent _typed;
    AppendedEvent _unknown;
    AppendedEvent _older;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.HasFor((EventTypeId)"display").Returns(true);
        _eventTypes.GetClrTypeFor((EventTypeId)"display").Returns(typeof(Display));
        _paths = ["count", "flag"];
        _response = new()
        {
            Context = new()
            {
                EventType = new EventType("display", 1).ToSequencesContract(),
                SequenceNumber = 42,
                EventSourceId = "source",
                EventSourceType = EventSourceType.Default,
                EventStreamType = EventStreamType.All,
                EventStreamId = EventStreamId.Default,
                CorrelationId = Guid.NewGuid(),
                Causation = [],
                CausedBy = new(),
                Tags = [],
                Subject = "subject"
            },
            Content = "{\"Count\":0,\"Flag\":false}",
            ErasedProperties = _paths
        };
    }

    void Because()
    {
        _typed = _response.ToClient("store", "tenant", _eventTypes, JsonSerializerOptions.Default);
        _paths.Add("laterMutation");
        _response.Context.EventType = new EventType("unknown", 1).ToSequencesContract();
        _unknown = _response.ToClient("store", "tenant", _eventTypes, JsonSerializerOptions.Default);
        _older = new Contracts.Sequences.AppendedEventResponse { Context = _response.Context, Content = "{}" }.ToClient("store", "tenant", _eventTypes, JsonSerializerOptions.Default);
    }

    [Fact] void should_keep_zero() => ((Display)_typed.Content).Count.ShouldEqual(0);
    [Fact] void should_keep_false() => ((Display)_typed.Content).Flag.ShouldBeFalse();
    [Fact] void should_snapshot_paths_on_context() => _typed.Context.ErasedProperties.ShouldEqual<IEnumerable<string>>(["count", "flag"]);
    [Fact] void should_forward_the_same_read_only_list() => ReferenceEquals(_typed.ErasedProperties, _typed.Context.ErasedProperties).ShouldBeTrue();
    [Fact] void should_deserialize_unknown_event_content() => _unknown.Content.ShouldBeOfExactType<ExpandoObject>();
    [Fact] void should_copy_paths_for_unknown_events() => _unknown.ErasedProperties.ShouldEqual<IEnumerable<string>>(["count", "flag", "laterMutation"]);
    [Fact] void should_default_missing_wire_metadata_to_empty() => _older.ErasedProperties.ShouldBeEmpty();
    [Fact] void should_preserve_context() => _typed.Context.Subject.Value.ShouldEqual("subject");

    record Display(int Count, bool Flag);
}
