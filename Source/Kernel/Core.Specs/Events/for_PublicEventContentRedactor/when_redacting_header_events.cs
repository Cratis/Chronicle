// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor;

public class when_redacting_header_events : Specification
{
    AppendedEvent[] _stored;
    AppendedEvent[] _public;
    string[] _before;

    void Establish()
    {
        _stored = given.header_events.All();
        _before = _stored.Select(@event => JsonSerializer.Serialize(@event, given.header_events.Options)).ToArray();
    }

    void Because() => _public = _stored.Select(@event => PublicEventContentRedactor.Redact(@event, given.header_events.Options)).ToArray();

    [Fact] void should_omit_every_content_variant() => _public.All(@event => !given.header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
    [Fact] void should_keep_header_names() => _public.All(@event => given.header_events.HasEmptyHeaders(JsonSerializer.Serialize(@event.Content))).ShouldBeTrue();
    [Fact] void should_keep_every_revision() => _public.All(@event => @event.Revisions.Count() == 2 && @event.Revisions.All(revision => given.header_events.HasEmptyHeaders(revision.Content))).ShouldBeTrue();
    [Fact] void should_keep_every_generation() => _public.All(@event => @event.GenerationalContent.Count == 2 && @event.GenerationalContent.Values.All(given.header_events.HasEmptyHeaders)).ShouldBeTrue();
    [Fact] void should_keep_original_content() => _public.All(@event => given.header_events.HasEmptyHeaders(@event.OriginalContent)).ShouldBeTrue();
    [Fact] void should_preserve_context_and_hashes() => _public.Zip(_stored).All(pair => ReferenceEquals(pair.First.Context, pair.Second.Context) && ReferenceEquals(pair.First.GenerationalHashes, pair.Second.GenerationalHashes)).ShouldBeTrue();
    [Fact] void should_preserve_revision_metadata() => _public.Zip(_stored).All(pair => pair.First.RevisedGeneration == pair.Second.RevisedGeneration && pair.First.ErasedProperties.SequenceEqual(pair.Second.ErasedProperties)).ShouldBeTrue();
    [Fact] void should_preserve_unrelated_values_and_offsets() => _public.Zip(_stored).All(pair => ((IDictionary<string, object?>)pair.First.Content)["moment"]!.Equals(((IDictionary<string, object?>)pair.Second.Content)["moment"])).ShouldBeTrue();
    [Fact] void should_not_change_durable_inputs() => _stored.Select(@event => JsonSerializer.Serialize(@event, given.header_events.Options)).ShouldEqual(_before);
    [Fact] void should_detach_public_content() => _public.Zip(_stored).All(pair => !ReferenceEquals(pair.First.Content, pair.Second.Content)).ShouldBeTrue();
}
