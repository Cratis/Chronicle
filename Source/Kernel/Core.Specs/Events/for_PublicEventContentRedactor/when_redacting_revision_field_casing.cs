// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor;

public class when_redacting_revision_field_casing : Specification
{
    string[] _envelopes;
    string[] _serialized;
    string[] _current;

    void Establish()
    {
        _envelopes = given.revised_header_events.All().Select(@event => @event.OriginalContent
            .Replace("\"eventType\"", "\"EVENTTYPE\"", StringComparison.Ordinal)
            .Replace("\"id\"", "\"ID\"", StringComparison.Ordinal)
            .Replace("\"content\"", "\"CONTENT\"", StringComparison.Ordinal)).ToArray();
    }

    void Because()
    {
        _serialized = _envelopes.Select(content => PublicEventContentRedactor.Redact(typeof(EventRevised).GetEventType(), content, given.header_events.Options)).ToArray();
        _current = _envelopes.Select(content => PublicEventContentRedactor.Redact(given.revised_header_events.All()[0] with { Content = JsonSerializer.Deserialize<ExpandoObject>(content)! }, given.header_events.Options))
            .Select(@event => JsonSerializer.Serialize(@event.Content)).ToArray();
    }

    [Fact] void should_redact_serialized_fields_independently_of_casing() => _serialized.All(content => given.header_events.HasEmptyHeaders(JsonElement.Parse(content).GetProperty("CONTENT").GetString()!)).ShouldBeTrue();
    [Fact] void should_redact_current_fields_independently_of_casing() => _current.All(content => given.header_events.HasEmptyHeaders(JsonElement.Parse(content).GetProperty("CONTENT").GetString()!)).ShouldBeTrue();
    [Fact] void should_omit_values_with_different_field_casing() => given.header_events.ContainsSentinel(string.Join(" ", _serialized.Concat(_current))).ShouldBeFalse();
}
