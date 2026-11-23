// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox;

public class when_the_same_registration_is_accepted_repeatedly : given.an_inbox
{
    RegistrationAcceptance[] _acceptances;
    int _savesAfterApplying;
    int _savesAfterRepeating;

    async Task Because()
    {
        ProjectionDefinition[] definitions = [CreateDefinition("projection")];
        _acceptances = [.. await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Accept(definitions, "studio")))];
        await WaitUntil(() => _intents.Intents.All(intent => intent.State == RegistrationIntentState.Applied));
        _savesAfterApplying = _intents.Saves;

        // Every namespace of the client, and every start, sends it again.
        var repeated = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Accept(definitions, "studio")));
        _acceptances = [.. _acceptances, .. repeated];
        _savesAfterRepeating = _intents.Saves;
    }

    [Fact] void should_give_every_caller_the_same_revision() => _acceptances.Select(acceptance => acceptance.Revision).Distinct().Count().ShouldEqual(1);
    [Fact] void should_write_one_intent_and_one_outcome() => _savesAfterApplying.ShouldEqual(2);
    [Fact] void should_write_nothing_for_a_repeat_of_what_was_applied() => _savesAfterRepeating.ShouldEqual(_savesAfterApplying);
    [Fact] void should_answer_applied_for_a_repeat_of_what_was_applied() => _acceptances[^1].State.ShouldEqual(RegistrationIntentState.Applied);
    [Fact] void should_apply_it_once() => _manager.ReceivedWithAnyArgs(1).RegisterShared(default!, default!, default, default, default, default);
}
