// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_unsubscribing;

public class and_subscribers_remain : given.a_subscribed_notifier
{
    async Task Because() => await _notifier.Unsubscribe(_subscribers[0]);

    [Fact] void should_not_deactivate() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Never);
}
