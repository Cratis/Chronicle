// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_deactivating;

public class and_the_activation_is_collected : given.a_subscriber_with_a_cached_pipeline
{
    Task Because() => _subscriber.OnDeactivateAsync(new(DeactivationReasonCode.ActivationIdle, string.Empty), CancellationToken.None);

    [Fact] void should_remove_the_definition_subscription() => _projectionGrain.Received(1).UnsubscribeDefinitionsChanged(Arg.Any<INotifyProjectionDefinitionsChanged>());
}
