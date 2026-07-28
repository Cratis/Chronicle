// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;
using ContractIWebhooks = Cratis.Chronicle.Contracts.Observation.Webhooks.IWebhooks;
using KernelWebhooks = Cratis.Chronicle.Services.Observation.Webhooks.Webhooks;

namespace Cratis.Chronicle.Services.Observation.Webhooks.for_Webhooks.given;

public class a_webhooks_service : Specification
{
    protected ContractIWebhooks _subject;
    protected IGrainFactory _grainFactory;
    protected IEventSequence _systemEventSequence;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _systemEventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_systemEventSequence);

        _subject = new KernelWebhooks(
            _grainFactory,
            Substitute.For<IStorage>(),
            Substitute.For<IWebhookDefinitionComparer>(),
            Substitute.For<IEncryption>(),
            Substitute.For<IOAuthClient>(),
            Substitute.For<IWebhookMediator>(),
            Options.Create(new ChronicleOptions()));
    }
}
