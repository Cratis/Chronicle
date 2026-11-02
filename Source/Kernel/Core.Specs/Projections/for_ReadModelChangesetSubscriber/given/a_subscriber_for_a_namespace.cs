// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ReadModelChangesetSubscriber.given;

public class a_subscriber_for_a_namespace : Specification
{
    protected ReadModelChangesetSubscriber _subscriber;
    protected IProjectionChangesetMediator _mediator;
    protected readonly Guid _subscriptionId = Guid.NewGuid();
    protected readonly EventStoreNamespaceName _namespace = "namespace-a";
    protected readonly ReadModelChangeContext _change = new(ReadModelChangeType.Added, 1, DateTimeOffset.UnixEpoch, CorrelationId.New());
    protected readonly JsonObject _readModel = new();

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _mediator = silo.AddService(Substitute.For<IProjectionChangesetMediator>());
        var key = new ObserverSubscriberKey("the-observer", "the-store", _namespace, EventSequenceId.Log, _subscriptionId.ToString("N"), string.Empty);
        _subscriber = await silo.CreateGrainAsync<ReadModelChangesetSubscriber>(key.ToString());
    }
}
