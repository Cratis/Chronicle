// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.for_ReadModelChangesetSubscriber.when_receiving_a_changeset;

public class and_it_is_for_another_namespace : given.a_subscriber_for_a_namespace
{
    async Task Because() => await _subscriber.OnChangeset("namespace-b", "key", _readModel, _change);

    [Fact] void should_not_forward_it_to_the_mediator() => _mediator.DidNotReceive().OnChangeset(Arg.Any<Guid>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
