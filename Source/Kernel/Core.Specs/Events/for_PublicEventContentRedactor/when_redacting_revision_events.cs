// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor;

public class when_redacting_revision_events : Specification
{
    AppendedEvent[] _stored;
    AppendedEvent[] _public;
    AppendedEvent[] _twice;
    string[] _before;

    void Establish()
    {
        _stored = given.revised_header_events.All();
        _before = _stored.Select(@event => JsonSerializer.Serialize(@event)).ToArray();
    }

    void Because()
    {
        _public = _stored.Select(@event => PublicEventContentRedactor.Redact(@event, given.header_events.Options)).ToArray();
        _twice = _public.Select(@event => PublicEventContentRedactor.Redact(@event, given.header_events.Options)).ToArray();
    }

    [Fact] void should_omit_every_embedded_content_variant() => _public.All(@event => !given.header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
    [Fact] void should_keep_current_header_names() => _public.All(@event => given.header_events.HasEmptyHeaders(JsonSerializer.Serialize(@event.Content))).ShouldBeTrue();
    [Fact] void should_keep_original_content() => _public.All(@event => given.header_events.HasEmptyHeaders(@event.OriginalContent)).ShouldBeTrue();
    [Fact] void should_keep_every_revision() => _public.All(@event => @event.Revisions.Count() == 2 && @event.Revisions.All(revision => given.header_events.HasEmptyHeaders(revision.Content))).ShouldBeTrue();
    [Fact] void should_keep_every_generation() => _public.All(@event => @event.GenerationalContent.Count == 2 && @event.GenerationalContent.Values.All(given.header_events.HasEmptyHeaders)).ShouldBeTrue();
    [Fact] void should_preserve_envelope_metadata() => _public.Zip(_stored).All(pair => JsonSerializer.Serialize(((IDictionary<string, object?>)pair.First.Content)["eventType"]) == JsonSerializer.Serialize(((IDictionary<string, object?>)pair.Second.Content)["eventType"])).ShouldBeTrue();
    [Fact] void should_preserve_context_and_hashes() => _public.Zip(_stored).All(pair => ReferenceEquals(pair.First.Context, pair.Second.Context) && ReferenceEquals(pair.First.GenerationalHashes, pair.Second.GenerationalHashes)).ShouldBeTrue();
    [Fact] void should_not_change_internal_values() => _stored.Select(@event => JsonSerializer.Serialize(@event)).ShouldEqual(_before);
    [Fact] void should_detach_public_content() => _public.Zip(_stored).All(pair => !ReferenceEquals(pair.First.Content, pair.Second.Content)).ShouldBeTrue();
    [Fact] void should_be_idempotent() => JsonSerializer.Serialize(_twice).ShouldEqual(JsonSerializer.Serialize(_public));
}
