// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying;

public class and_a_protected_snapshot_has_no_release_owner : and_the_pipeline_owns_a_released_snapshot
{
    protected override bool ProvidesReleasedSnapshot => false;

    [Fact] void should_fail_instead_of_guessing_release_ownership() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_publish_unowned_protected_state() => _notified.ShouldBeNull();
}
