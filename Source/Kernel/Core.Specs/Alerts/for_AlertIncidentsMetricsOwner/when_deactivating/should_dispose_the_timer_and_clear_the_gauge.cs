// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwner.when_deactivating;

public class should_dispose_the_timer_and_clear_the_gauge : given.an_owner
{
    IGrainTimer _timer;

    void Establish()
    {
        _timer = Substitute.For<IGrainTimer>();
        typeof(AlertIncidentsMetricsOwner).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_owner, _timer);
    }

    async Task Because() => await _owner.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);

    [Fact] void should_dispose_the_timer() => _timer.Received(1).Dispose();
    [Fact] void should_clear_the_gauge_for_itself() => _gauge.Received(1).Clear(_owner);
}
