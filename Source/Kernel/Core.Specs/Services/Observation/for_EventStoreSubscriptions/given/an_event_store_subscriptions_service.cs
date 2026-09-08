// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;
using ContractIEventStoreSubscriptions = Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions;
using KernelEventStoreSubscriptions = Cratis.Chronicle.Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions;

namespace Cratis.Chronicle.Services.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.given;

public class an_event_store_subscriptions_service : Specification
{
    protected ContractIEventStoreSubscriptions _subject;
    protected IGrainFactory _grainFactory;
    protected IEventSequence _systemEventSequence;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _systemEventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_systemEventSequence);

        _subject = new KernelEventStoreSubscriptions(
            _grainFactory,
            Substitute.For<IStorage>(),
            Options.Create(new ChronicleOptions()));
    }
}
