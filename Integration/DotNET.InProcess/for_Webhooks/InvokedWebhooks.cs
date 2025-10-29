// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.InProcess.Integration.for_Webhooks;

public class InvokedWebhooks
{
    readonly ConcurrentBag<string> _invokedWebhooks = new();

    public void Add(string webhookPayload)
    {
        _invokedWebhooks.Add(webhookPayload);
    }

    public IEnumerable<string> GetAll() => _invokedWebhooks;

    public bool HasAny() => !_invokedWebhooks.IsEmpty;

    public int Count => _invokedWebhooks.Count;
}