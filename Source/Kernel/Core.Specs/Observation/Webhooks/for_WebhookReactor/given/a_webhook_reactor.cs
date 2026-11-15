// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookReactor.given;

public class a_webhook_reactor : Specification
{
    protected WebhookReactor _reactor;
    protected IGrainFactory _grainFactory;
    protected ILogger<WebhookReactor> _logger;
    protected IWebhooks _webhooksManager;
    protected IWebhookSigningKeyRing _signingKeyRing;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _logger = Substitute.For<ILogger<WebhookReactor>>();
        _webhooksManager = Substitute.For<IWebhooks>();
        _signingKeyRing = Substitute.For<IWebhookSigningKeyRing>();
        _grainFactory.GetGrain<IWebhookSigningKeyRing>(Arg.Any<string>()).Returns(_signingKeyRing);
        _grainFactory.GetGrain<IWebhooks>(Arg.Any<string>()).Returns(_webhooksManager);
        _reactor = new WebhookReactor(_grainFactory, _logger);
    }
}
