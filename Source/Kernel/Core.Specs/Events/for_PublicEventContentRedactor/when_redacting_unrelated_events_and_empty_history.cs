// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor;

public class when_redacting_unrelated_events_and_empty_history : Specification
{
    AppendedEvent _unrelated;
    AppendedEvent _protected;
    AppendedEvent _result;
    AppendedEvent _emptyHistory;
    string _unrelatedJson;

    void Establish()
    {
        _unrelated = given.header_events.All()[0] with { Context = EventContext.Empty with { EventType = new("ApplicationEvent", 1) } };
        _protected = given.header_events.All()[0] with { OriginalContent = string.Empty, Revisions = [new(1, CorrelationId.NotSet, [], Concepts.Identities.Identity.System, DateTimeOffset.UnixEpoch)], GenerationalContent = new Dictionary<int, string> { [1] = string.Empty } };
    }

    void Because()
    {
        _result = PublicEventContentRedactor.Redact(_unrelated, given.header_events.Options);
        _emptyHistory = PublicEventContentRedactor.Redact(_protected, given.header_events.Options);
        _unrelatedJson = PublicEventContentRedactor.Redact(_unrelated.Context.EventType, " current-header-value ", given.header_events.Options);
    }

    [Fact] void should_keep_unrelated_events_by_reference() => _result.ShouldBeSame(_unrelated);
    [Fact] void should_not_interpret_unrelated_json() => _unrelatedJson.ShouldEqual(" current-header-value ");
    [Fact] void should_keep_optional_empty_original_content() => _emptyHistory.OriginalContent.ShouldEqual(string.Empty);
    [Fact] void should_keep_optional_empty_revisions() => _emptyHistory.Revisions.Single().Content.ShouldEqual(string.Empty);
    [Fact] void should_keep_optional_empty_generations() => _emptyHistory.GenerationalContent[1].ShouldEqual(string.Empty);
}
