// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

public class appended_contexts : Specification
{
    protected EventSourceId _firstId;
    protected EventSourceId _secondId;
    protected EventContext _context;

    void Establish()
    {
        _firstId = EventSourceId.New();
        _secondId = EventSourceId.New();
        _context = EventContext.From(
            "context-store",
            "context-namespace",
            Defaults.Instance.EventTypes.GetEventTypeFor(typeof(ScenarioContextRecorded)),
            "context-source",
            _firstId,
            "context-stream",
            "stream-42",
            2UL,
            CorrelationId.New(),
            new DateTimeOffset(2024, 3, 2, 10, 15, 30, TimeSpan.FromHours(2))) with
        {
            Causation = [new Causation(new DateTimeOffset(2024, 3, 2, 10, 15, 0, TimeSpan.FromHours(2)), "context-command", new Dictionary<string, string> { ["input"] = "value" })],
            CausedBy = Identity.System,
            Tags = [new Tag("context-tag")],
            Subject = new Subject("context-subject")
        };
    }
}
