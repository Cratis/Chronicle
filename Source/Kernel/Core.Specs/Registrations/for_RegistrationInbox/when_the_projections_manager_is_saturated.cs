// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox;

public class when_the_projections_manager_is_saturated : given.an_inbox
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    RegistrationAcceptance _acceptance;
    RegistrationStatus _whileSaturated;
    RegistrationStatus _afterwards;
    int _savedBeforeRelease;
    bool _acceptedWhileManagerWasHeld;

    protected override void PrepareBeforeActivation() => _manager.RegisterShared(default!, default!, default, default, default, default).ReturnsForAnyArgs(_ => _release.Task);

    async Task Because()
    {
        var accepting = Accept([CreateDefinition("projection")], "studio");
        using var timeout = new CancellationTokenSource();
        _acceptedWhileManagerWasHeld = await Task.WhenAny(accepting, Task.Delay(TimeSpan.FromSeconds(1), TimeProvider.System, timeout.Token)) == accepting;
        await timeout.CancelAsync();
        _acceptance = await accepting;
        _savedBeforeRelease = _intents.Saves;
        _whileSaturated = await _grain.GetStatus(RegistrationKind.Projections, "studio");
        _release.SetResult();
        await WaitUntil(() => _intents.Intents.All(intent => intent.State == RegistrationIntentState.Applied));
        _afterwards = await _grain.GetStatus(RegistrationKind.Projections, "studio");
    }

    [Fact] void should_accept_while_the_manager_is_held() => _acceptedWhileManagerWasHeld.ShouldBeTrue();
    [Fact] void should_accept_as_pending() => _acceptance.State.ShouldEqual(RegistrationIntentState.Pending);
    [Fact] void should_write_one_intent() => _savedBeforeRelease.ShouldEqual(1);
    [Fact] void should_report_pending_while_the_manager_is_held() => _whileSaturated.State.ShouldEqual(RegistrationIntentState.Pending);
    [Fact] void should_apply_it_afterwards() => _afterwards.State.ShouldEqual(RegistrationIntentState.Applied);
    [Fact] void should_report_the_applied_revision() => _afterwards.AppliedRevision.ShouldEqual(_acceptance.Revision);
    [Fact] void should_apply_it_once() => _manager.ReceivedWithAnyArgs(1).RegisterShared(default!, default!, default, default, default, default);
    [Fact] void should_drop_the_payload_once_applied() => _intents.Intents.Single().Payload.Length.ShouldEqual(0);
}
