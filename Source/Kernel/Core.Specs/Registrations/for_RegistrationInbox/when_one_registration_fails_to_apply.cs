// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox;

public class when_one_registration_fails_to_apply : given.an_inbox
{
    readonly List<string> _applied = [];
    RegistrationStatus _failing;
    RegistrationStatus _healthy;
    RegistrationStatus _failingAfterFirstAttempt;
    RegistrationAcceptance _acceptedAgain;
    int _attempts;

    protected override void PrepareBeforeActivation() => _manager.RegisterShared(default!, default!, default, default, default, default).ReturnsForAnyArgs(call =>
    {
        var identifier = call.ArgAt<IEnumerable<ProjectionDefinition>>(1).Single().Identifier.Value;
        if (identifier == "broken")
        {
            _attempts++;
            return Task.FromException(new InvalidOperationException("the read model schema conflicts"));
        }

        _applied.Add(identifier);
        return Task.CompletedTask;
    });

    async Task Because()
    {
        await Accept([CreateDefinition("broken")], "broken-app");
        await Accept([CreateDefinition("fine")], "fine-app");
        await WaitUntil(() => _applied.Contains("fine") && _attempts == 1);
        _failingAfterFirstAttempt = await _grain.GetStatus(RegistrationKind.Projections, "broken-app");

        // The retry is due once the delay has passed.
        _clock.Advance(TimeSpan.FromSeconds(6));
        await WaitUntil(() => _attempts == 2 && _intents.Intents.Any(intent => intent.State == RegistrationIntentState.Failed));
        _failing = await _grain.GetStatus(RegistrationKind.Projections, "broken-app");
        _healthy = await _grain.GetStatus(RegistrationKind.Projections, "fine-app");

        // Registering it again, unchanged, retries it.
        _acceptedAgain = await Accept([CreateDefinition("broken")], "broken-app");
    }

    [Fact] void should_apply_the_other_registration_regardless() => _applied.ShouldContain("fine");
    [Fact] void should_report_the_healthy_registrant_as_applied() => _healthy.State.ShouldEqual(RegistrationIntentState.Applied);
    [Fact] void should_keep_it_pending_while_it_has_attempts_left() => _failingAfterFirstAttempt.State.ShouldEqual(RegistrationIntentState.Pending);
    [Fact] void should_report_it_as_failed_once_the_attempts_are_used() => _failing.State.ShouldEqual(RegistrationIntentState.Failed);
    [Fact] void should_say_why_it_failed() => _failing.Failure.ShouldContain("the read model schema conflicts");
    [Fact] void should_retry_it_before_giving_up() => _attempts.ShouldBeGreaterThanOrEqual(2);
    [Fact] void should_accept_the_same_registration_again_as_a_new_revision() => _acceptedAgain.State.ShouldEqual(RegistrationIntentState.Pending);
}
