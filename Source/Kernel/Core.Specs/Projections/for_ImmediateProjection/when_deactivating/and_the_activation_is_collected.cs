// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_deactivating;

public class and_the_activation_is_collected : given.an_immediate_projection
{
    Task Because() => _grain.OnDeactivateAsync(new(DeactivationReasonCode.ActivationIdle, string.Empty), CancellationToken.None);

    [Fact] void should_remove_the_definition_subscription() => _projection.Received(1).UnsubscribeDefinitionsChanged(Arg.Any<INotifyProjectionDefinitionsChanged>());
}
