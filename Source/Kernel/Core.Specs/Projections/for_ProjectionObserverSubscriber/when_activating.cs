// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_activating : given.a_subscriber_with_a_cached_pipeline
{
    Task Because() => _subscriber.OnActivateAsync(CancellationToken.None);

    [Fact] async Task should_not_write_shared_definition_state() => await _stateStorage.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_not_read_partition_definition_state() => await _stateStorage.DidNotReceive().ReadStateAsync();
}
