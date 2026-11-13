// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_unsubscribing;

public class and_it_was_the_last_subscriber : given.a_subscribed_notifier
{
    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        await _notifier.Unsubscribe(_subscribers[0]);
        await _notifier.Unsubscribe(_subscribers[1]);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_publish_that_the_projection_is_no_longer_watched() => _projectionsServiceClient.Received(1).SetWatchState(_eventStore, _namespace, _projection, false);
    [Fact] void should_deactivate_when_idle() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Once);
}
