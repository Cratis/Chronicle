// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.InProcess.Integration.for_Webhooks;

public class InvokedWebhooks
{
    readonly ConcurrentBag<(string Body, Dictionary<string, string> Headers)> _invokedWebhooks = new();

    public void Add(string requestBody, Dictionary<string, string> requestHeaders)
    {
        _invokedWebhooks.Add((requestBody, requestHeaders));
    }

    public IEnumerable<(string Body, Dictionary<string, string> Headers)> GetAll() => _invokedWebhooks;

    public bool HasAny() => !_invokedWebhooks.IsEmpty;

    public int Count => _invokedWebhooks.Count;

    public async Task WaitForInvocation(int count)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (_invokedWebhooks.Count < count)
        {
            await Task.Delay(100).WaitAsync(cts.Token);
        }
        Console.WriteLine("Hello");
    }
}
