// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.for_ReadModelChangesetSubscriber.when_receiving_a_changeset;

public class and_it_is_for_its_namespace : given.a_subscriber_for_a_namespace
{
    async Task Because() => await _subscriber.OnChangeset(_namespace, "key", _readModel, _change);

    [Fact] void should_forward_it_to_the_mediator() => _mediator.Received(1).OnChangeset(_subscriptionId, _namespace, (ReadModelKey)"key", _readModel, _change);
}
