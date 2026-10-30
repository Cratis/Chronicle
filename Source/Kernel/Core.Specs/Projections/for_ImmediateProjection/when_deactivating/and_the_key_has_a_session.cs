// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_deactivating;

public class and_the_key_has_a_session : given.a_session_immediate_projection
{
    Task Because() => _grain.OnDeactivateAsync(new(DeactivationReasonCode.ActivationIdle, string.Empty), CancellationToken.None);

    [Fact] void should_dispose_the_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] void should_remove_the_definition_subscription() => _projection.Received(1).UnsubscribeDefinitionsChanged(Arg.Any<INotifyProjectionDefinitionsChanged>());
}
