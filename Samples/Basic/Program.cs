// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis;
using Basic;

using var client = new CratisClient("cratis://localhost:35000");
var eventStore = client.GetEventStore(Guid.Empty.ToString());
await eventStore.EventTypes.Discover();
await eventStore.Observers.Discover();
await eventStore.Reducers.Discover();
await eventStore.Projections.Discover();
await eventStore.EventLog.Append(
    eventSourceId: Guid.NewGuid(),
    new ItemAddedToCart(
        PersonId: new(Guid.NewGuid()),
        MaterialId: new(Guid.NewGuid()),
        Quantity: 1));
