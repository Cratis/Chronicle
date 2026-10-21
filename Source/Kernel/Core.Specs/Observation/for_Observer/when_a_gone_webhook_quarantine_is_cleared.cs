// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_webhook_quarantine_is_cleared : given.a_gone_retired_webhook
{
    async Task Because() => await _observer.ClearObserverQuarantine();

    [Fact] void should_remain_retired() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_keep_the_incident() => _stateStorage.State.WebhookRetirement.Id.ShouldEqual(_retirementId);
}
