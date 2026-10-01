// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class and_quarantine_identity_is_already_persisted : given.an_observer
{
    Guid? _episode;
    Guid _lifecycle;

    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        _episode = _stateStorage.State.QuarantineEpisodeId;
        _lifecycle = _stateStorage.State.AlertLifecycleId;
    }

    async Task Because() => await Crash();

    [Fact] void should_preserve_the_episode() => _stateStorage.State.QuarantineEpisodeId.ShouldEqual(_episode);
    [Fact] void should_have_allocated_an_identity_before_the_crash() => _episode.ShouldNotBeNull();
    [Fact] void should_not_replace_the_lifecycle_on_activation() => _stateStorage.State.AlertLifecycleId.ShouldEqual(_lifecycle);
}
