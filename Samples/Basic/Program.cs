// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis;
using Basic;

using var client = new CratisClient("cratis://localhost:35000");
var eventStore = client.GetEventStore(Guid.Empty.ToString());
await eventStore.EventTypes.RegisterKnownEventTypes();
await eventStore.Observers.RegisterKnownObservers();
await eventStore.Reducers.RegisterKnownReducers();
await eventStore.Projections.RegisterKnownProjections();
await eventStore.EventLog.Append(
    eventSourceId: Guid.NewGuid(),
    new ItemAddedToCart(
        PersonId: new(Guid.NewGuid()),
        MaterialId: new(Guid.NewGuid()),
        Quantity: 1));
