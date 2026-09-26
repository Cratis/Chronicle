// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_using_per_run_constraint_artifacts : Specification, IDisposable
{
    EventScenario _withConstraint;
    EventScenario _withoutConstraint;
    AppendResult _constrainedResult;
    AppendResult _unconstrainedResult;

    void Establish()
    {
        var constrainedArtifacts = Substitute.For<IClientArtifactsProvider>();
        constrainedArtifacts.EventTypes.Returns([typeof(SubscriptionActivated)]);
        constrainedArtifacts.UniqueEventTypeConstraints.Returns([typeof(SubscriptionActivated)]);
        _withConstraint = new EventScenario(new Defaults(constrainedArtifacts));

        var unconstrainedArtifacts = Substitute.For<IClientArtifactsProvider>();
        unconstrainedArtifacts.EventTypes.Returns([typeof(SubscriptionActivated)]);
        _withoutConstraint = new EventScenario(new Defaults(unconstrainedArtifacts));
    }

    async Task Because()
    {
        var id = EventSourceId.New();
        await _withConstraint.When.ForEventSource(id).Events(new SubscriptionActivated("pro"));
        _constrainedResult = await _withConstraint.When.ForEventSource(id).Events(new SubscriptionActivated("pro"));

        await _withoutConstraint.When.ForEventSource(id).Events(new SubscriptionActivated("pro"));
        _unconstrainedResult = await _withoutConstraint.When.ForEventSource(id).Events(new SubscriptionActivated("pro"));
    }

    [Fact] void should_apply_the_first_runs_constraint() => _constrainedResult.ShouldHaveConstraintViolation(SubscriptionActivated.ConstraintName);
    [Fact] void should_not_leak_the_constraint_to_the_second_run() => _unconstrainedResult.ShouldBeSuccessful();

    public void Dispose()
    {
        _withConstraint.Dispose();
        _withoutConstraint.Dispose();
    }
}
