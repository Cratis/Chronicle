// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox;

public class when_registrations_wait_to_be_applied : given.an_inbox
{
    readonly TaskCompletionSource _release = new();
    DateTimeOffset _firstAcceptedAt;
    DateTimeOffset _secondAcceptedAt;

    protected override void PrepareBeforeActivation() => _manager.RegisterShared(default!, default!, default, default, default, default).ReturnsForAnyArgs(_ => _release.Task);

    async Task Because()
    {
        _firstAcceptedAt = _clock.GetUtcNow();
        await Accept([CreateDefinition("first")], "first-app");
        _clock.Advance(TimeSpan.FromSeconds(30));
        _secondAcceptedAt = _clock.GetUtcNow();
        await Accept([CreateDefinition("second")], "second-app");
        _release.SetResult();
        await WaitUntil(() => _intents.Intents.All(intent => intent.State == Storage.Registrations.RegistrationIntentState.Applied));
        await WaitUntil(() => _backlog.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IRegistrationBacklog.Report) && (int)call.GetArguments()[1]! == 0));
    }

    [Fact] void should_report_both_registrations_while_they_wait() => _backlog.Received().Report(new EventStoreName(EventStore), 2, _firstAcceptedAt);
    [Fact] void should_report_the_acceptance_of_the_oldest_registration_not_the_newest() => _backlog.DidNotReceive().Report(Arg.Any<EventStoreName>(), 2, _secondAcceptedAt);
    [Fact] void should_report_nothing_waiting_once_they_are_applied() => _backlog.Received().Report(new EventStoreName(EventStore), 0, null);
}
