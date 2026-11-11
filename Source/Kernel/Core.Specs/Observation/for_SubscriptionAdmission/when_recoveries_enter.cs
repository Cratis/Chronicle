// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_SubscriptionAdmission;

public class when_recoveries_enter : Specification
{
    readonly SubscriptionAdmission _admission = new(4, Environment.ProcessorCount);
    readonly SubscriptionAdmission _admissionOfOne = new(1, Environment.ProcessorCount);
    readonly List<bool> _recoveriesEntered = [];
    readonly List<bool> _subscriptionsEntered = [];
    bool _recoveryEnteredAfterARecoveryReleased;
    bool _recoveryEnteredWithALimitOfOne;

    async Task Because()
    {
        for (var i = 0; i < 3; i++) _recoveriesEntered.Add(_admission.TryEnterForRecovery());
        for (var i = 0; i < 2; i++) _subscriptionsEntered.Add(await _admission.WaitAsync(TimeSpan.Zero, CancellationToken.None));

        _admission.Release();
        _admission.ReleaseRecovery();
        _recoveryEnteredAfterARecoveryReleased = _admission.TryEnterForRecovery();

        _recoveryEnteredWithALimitOfOne = _admissionOfOne.TryEnterForRecovery();
    }

    [Fact] void should_admit_recoveries_up_to_half_the_limit() => _recoveriesEntered.Count(_ => _).ShouldEqual(2);
    [Fact] void should_turn_away_the_recovery_beyond_its_share() => _recoveriesEntered[2].ShouldBeFalse();
    [Fact] void should_keep_the_rest_for_subscriptions() => _subscriptionsEntered.Count(_ => _).ShouldEqual(2);
    [Fact] void should_have_half_the_limit_for_recoveries() => _admission.RecoveryLimit.ShouldEqual(2);
    [Fact] void should_admit_a_recovery_again_once_one_is_released() => _recoveryEnteredAfterARecoveryReleased.ShouldBeTrue();
    [Fact] void should_admit_at_least_one_recovery() => _recoveryEnteredWithALimitOfOne.ShouldBeTrue();
}
