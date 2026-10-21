// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_an_earlier_per_value_claim_is_claimed_by_another_event_source : Specification, IDisposable
{
    const string ConstraintName = "RetainedScenarioVersion";
    EventScenario _scenario;
    AppendResult _result;
    string _earlierValue;

    async Task Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(ScenarioVersionAdded)]);
        artifacts.UniqueConstraints.Returns([typeof(ScenarioVersionAdded)]);
        _scenario = new EventScenario(new Defaults(artifacts));
        _earlierValue = Guid.NewGuid().ToString();
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(
            new ScenarioVersionAdded(_earlierValue),
            new ScenarioVersionAdded(Guid.NewGuid().ToString()));
    }

    async Task Because() => _result = await _scenario.When.ForEventSource(EventSourceId.New()).Events(new ScenarioVersionAdded(_earlierValue));

    [Fact] void should_reject_the_earlier_value() => _result.ShouldHaveConstraintViolation(ConstraintName);
    [Fact] void should_not_succeed() => _result.ShouldBeFailed();

    public void Dispose() => _scenario.Dispose();

    /// <summary>
    /// Records one of the version values retained by the event source.
    /// </summary>
    /// <param name="Value">The version value.</param>
    [EventType]
    public record ScenarioVersionAdded([property: Unique(ConstraintName, Mode = UniqueConstraintMode.PerValue)] string Value);
}
