// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

namespace Cratis.Chronicle.Services.Events.for_AppendedEventConverters;

public class when_converting_header_events : Specification
{
    Concepts.Events.AppendedEvent[] _stored;
    Contracts.Events.AppendedEvent[] _results;

    void Establish() => _stored = header_events.All();

    void Because() => _results = _stored.ToContract(header_events.Options).ToArray();

    [Fact] void should_redact_all_content_variants() => _results.All(@event => !header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
    [Fact] void should_keep_current_header_names() => _results.All(@event => header_events.HasEmptyHeaders(@event.Content)).ShouldBeTrue();
    [Fact] void should_keep_original_content() => _results.All(@event => header_events.HasEmptyHeaders(@event.OriginalContent)).ShouldBeTrue();
    [Fact] void should_keep_revision_content() => _results.All(@event => @event.Revisions.Count == 2 && @event.Revisions.All(revision => header_events.HasEmptyHeaders(revision.Content))).ShouldBeTrue();
    [Fact] void should_keep_generation_content() => _results.All(@event => @event.GenerationalContent.Count == 2 && @event.GenerationalContent.Values.All(header_events.HasEmptyHeaders)).ShouldBeTrue();
    [Fact] void should_leave_internal_content_available() => _stored.All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
}
