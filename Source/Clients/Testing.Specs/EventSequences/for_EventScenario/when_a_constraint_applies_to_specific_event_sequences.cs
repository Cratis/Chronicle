// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A scenario appends to the event log, so a constraint scoped to the event log is enforced there and one scoped to
/// the outbox is not. The second half is what shows the declaration reaches the in-process kernel at all: were it
/// dropped on the way, both constraints would apply everywhere and both appends would be refused.
/// </summary>
public class when_a_constraint_applies_to_specific_event_sequences : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _logScopedResult;
    AppendResult _outboxScopedResult;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(NewsletterSubscribedInLog), typeof(NewsletterSubscribedInOutbox)]);
        artifacts.UniqueEventTypeConstraints.Returns([typeof(NewsletterSubscribedInLog), typeof(NewsletterSubscribedInOutbox)]);
        _scenario = new EventScenario(new Defaults(artifacts));
    }

    async Task Because()
    {
        var id = EventSourceId.New();
        await _scenario.When.ForEventSource(id).Events(new NewsletterSubscribedInLog("weekly"));
        _logScopedResult = await _scenario.When.ForEventSource(id).Events(new NewsletterSubscribedInLog("weekly"));

        await _scenario.When.ForEventSource(id).Events(new NewsletterSubscribedInOutbox("weekly"));
        _outboxScopedResult = await _scenario.When.ForEventSource(id).Events(new NewsletterSubscribedInOutbox("weekly"));
    }

    [Fact] void should_enforce_the_constraint_scoped_to_the_event_log() => _logScopedResult.ShouldHaveConstraintViolation(NewsletterSubscribedInLog.ConstraintName);
    [Fact] void should_not_enforce_the_constraint_scoped_to_the_outbox() => _outboxScopedResult.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
