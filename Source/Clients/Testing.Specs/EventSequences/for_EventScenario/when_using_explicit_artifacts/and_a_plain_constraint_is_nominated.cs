// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_using_explicit_artifacts;

public class and_a_plain_constraint_is_nominated : Specification
{
    EventScenario _scenario;
    EventSourceId _id;
    AppendResult _duplicate;

    async Task Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<Claimed>(_ => _.WithId("plain-claim")).RegisterConstraint<Once>();
        _scenario = new(new Defaults(new InMemoryClientArtifactsProvider(), null, options));
        _id = EventSourceId.New();
        (await _scenario.EventLog.Append(_id, new Claimed())).ShouldBeSuccessful();
    }

    async Task Because() => _duplicate = await _scenario.EventLog.Append(_id, new Claimed());
    void Destroy() => _scenario.Dispose();

    [Fact] void should_enforce_the_real_append_constraint() => _duplicate.ShouldHaveConstraintViolations();

    public record Claimed;
    public class Once
    {
        public void Define(IConstraintBuilder builder) => builder.Unique<Claimed>();
    }
}
