// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned.given;

public class a_pinned_client(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
{
#pragma warning disable CA2213 // The specification lifecycle invokes Destroy(), which disposes this client.
    ChronicleClient _client;
#pragma warning restore CA2213
    protected IEventStore _pinnedStore;

    async Task Establish()
    {
        // The fixture reuses its client and event store. Configure a separate client before creating its store,
        // rather than applying PostConfigure to the fixture's already-cached options and observers.
        _client = new ChronicleClient(
            new BorrowedConnection(EventStore.Connection),
            new ChronicleOptions
            {
                EventGenerationDelivery = EventGenerationDelivery.Pinned,
                DefaultSinkTypeId = Services.GetRequiredService<IChronicleClient>().Options.DefaultSinkTypeId
            },
            artifactsProvider: this,
            serviceProvider: Services);
        _pinnedStore = await _client.GetEventStore($"pinned-{Guid.NewGuid():N}", EventStore.Namespace);
        await _pinnedStore.RegisterAll();
        _pinnedStore.Registration.IsSuccess.ShouldBeTrue();
    }

    void Destroy()
    {
        _client?.EvictEventStores();
        _client?.Dispose();
    }
}
