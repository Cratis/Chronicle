// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_AppendedEvent;

public class when_deconstructing_with_original_named_arguments : Specification
{
    AppendedEvent _event;
    string _id;
    EventContext _context;
    string _content;
    string _originalContent;
    IEnumerable<EventRevision> _revisions;
    IEnumerable<KeyValuePair<int, string>> _generationalContent;
    uint? _revisedGeneration;

    void Establish()
    {
        var identity = new Identity("subject", "name", "username", null);
        var context = new EventContext(
            new EventType("event-type", 1, false),
            "source-type",
            "source",
            42,
            "stream-type",
            "stream",
            DateTimeOffset.UnixEpoch,
            Guid.Empty,
            [],
            identity,
            [],
            EventHash.NotSet,
            EventObservationState.Initial);
        _event = new AppendedEvent(
            Id: "42",
            Context: context,
            Content: "{\"current\":true}",
            OriginalContent: "{\"original\":true}",
            Revisions: [new EventRevision(2, "correlation", identity, DateTimeOffset.UnixEpoch, "{}")],
            GenerationalContent: [new KeyValuePair<int, string>(1, "{}")],
            RevisedGeneration: 2);
    }

    void Because() => _event.Deconstruct(
        Id: out _id,
        Context: out _context,
        Content: out _content,
        OriginalContent: out _originalContent,
        Revisions: out _revisions,
        GenerationalContent: out _generationalContent,
        RevisedGeneration: out _revisedGeneration);

    [Fact] void should_preserve_the_id() => _id.ShouldEqual(_event.Id);
    [Fact] void should_preserve_the_context() => _context.ShouldEqual(_event.Context);
    [Fact] void should_preserve_the_content() => _content.ShouldEqual(_event.Content);
    [Fact] void should_preserve_the_original_content() => _originalContent.ShouldEqual(_event.OriginalContent);
    [Fact] void should_preserve_the_revisions() => _revisions.ShouldEqual(_event.Revisions);
    [Fact] void should_preserve_the_generational_content() => _generationalContent.ShouldEqual(_event.GenerationalContent);
    [Fact] void should_preserve_the_revised_generation() => _revisedGeneration.ShouldEqual(_event.RevisedGeneration);
    [Fact] void should_leave_erased_properties_empty_for_the_legacy_constructor() => _event.ErasedProperties.ShouldBeEmpty();
}
