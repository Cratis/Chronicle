// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.InProcess.Integration.for_Webhooks;

[Route("/webhooks")]
public class WebhookController(InvokedWebhooks invokedWebhooks) : ControllerBase
{
    [HttpPost]
    public Task ReceiveWebhook()
    {
        var request = HttpContext.Request;
        invokedWebhooks.Requests.Add(request);
        return Task.CompletedTask;
    }
}