// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

namespace Cratis.Chronicle.Sequences.for_EventSequencePage;

public class when_reading_header_events : given.a_page_query
{
    void Establish()
    {
        dynamic ordinary = new ExpandoObject();
        ordinary.targetHeaders = new Dictionary<string, string> { ["X-Ordinary"] = "ordinary-value" };
        _window = [.. header_events.All(), new(Concepts.Events.EventContext.Empty with { EventType = new("ApplicationEvent", 1), SequenceNumber = 30 }, (ExpandoObject)ordinary)];
    }

    async Task Because() => _page = await Query(count: _window.Length);

    [Fact] void should_redact_the_public_page() => header_events.ContainsSentinel(JsonSerializer.Serialize(_page)).ShouldBeFalse();
    [Fact] void should_keep_events_and_headers() => _page.Events.Take(_window.Length - 1).All(@event => header_events.HasEmptyHeaders(@event.Content)).ShouldBeTrue();
    [Fact] void should_release_real_content_before_public_conversion() => _released.Take(_window.Length - 1).All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
    [Fact] void should_keep_ordinary_event_headers() => JsonElement.Parse(_page.Events.Last().Content).GetProperty("targetHeaders").GetProperty("X-Ordinary").GetString().ShouldEqual("ordinary-value");
    [Fact] void should_preserve_stored_input() => _window.Take(_window.Length - 1).All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
}
