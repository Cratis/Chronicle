// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox;

public class when_a_registration_is_retried : given.an_inbox
{
    DateTimeOffset _acceptedAt;
    int _attempts;

    protected override void PrepareBeforeActivation() => _manager.RegisterShared(default!, default!, default, default, default, default).ReturnsForAnyArgs(_ =>
    {
        _attempts++;
        return Task.FromException(new InvalidOperationException("the read model schema conflicts"));
    });

    async Task Because()
    {
        _acceptedAt = _clock.GetUtcNow();
        await Accept([CreateDefinition("broken")], "broken-app");
        await WaitUntil(() => _attempts == 1);
        _clock.Advance(TimeSpan.FromSeconds(6));
        await WaitUntil(() => _intents.Intents.Any(intent => intent.State == RegistrationIntentState.Failed));
    }

    [Fact] void should_keep_counting_from_when_it_was_accepted_while_it_is_retried() => _backlog.Received().Report(new EventStoreName(EventStore), 1, _acceptedAt);
    [Fact] void should_not_start_counting_again_when_an_attempt_fails() => _backlog.DidNotReceive().Report(new EventStoreName(EventStore), 1, Arg.Is<DateTimeOffset?>(at => at > _acceptedAt));
    [Fact] void should_report_nothing_waiting_once_it_is_recorded_as_failed() => _backlog.Received().Report(new EventStoreName(EventStore), 0, null);
}
