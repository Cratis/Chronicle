// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace Cratis.Chronicle.Integration.for_Webhooks.when_generation_is_pinned;

/// <summary>
/// A webhook receiver owned by a single test context. It is the transport the kernel sends to, so it never
/// touches the web host shared between contexts, and every held response is released on disposal.
/// </summary>
public sealed class PinnedWebhookReceiver : HttpMessageHandler
{
    readonly Channel<(JsonObject Event, TaskCompletionSource Response)> _deliveries = Channel.CreateUnbounded<(JsonObject, TaskCompletionSource)>();
    readonly ConcurrentQueue<TaskCompletionSource> _gates = new();
    volatile bool _failDelivery;

    /// <summary>
    /// Sets whether deliveries received from now on are answered with a 400.
    /// </summary>
    /// <param name="fail">True to answer with 400, false to answer with 200.</param>
    public void FailDeliveries(bool fail) => _failDelivery = fail;

    /// <summary>
    /// Waits for the next delivered event, bounded to 30 seconds.
    /// </summary>
    /// <returns>The event and the gate that holds its HTTP response until set.</returns>
    public async Task<(JsonObject Event, TaskCompletionSource Response)> NextDelivery()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await _deliveries.Reader.ReadAsync(cancellation.Token);
    }

    /// <summary>
    /// Releases every response gate that is still held.
    /// </summary>
    public void ReleaseAll()
    {
        while (_gates.TryDequeue(out var gate))
        {
            gate.TrySetResult();
        }
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var payload = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!.AsObject();
        var response = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _gates.Enqueue(response);
        var statusCode = _failDelivery ? System.Net.HttpStatusCode.BadRequest : System.Net.HttpStatusCode.OK;
        foreach (var @event in payload["events"]!.AsArray())
        {
            await _deliveries.Writer.WriteAsync((@event!.AsObject(), response), cancellationToken);
        }

        await response.Task.WaitAsync(cancellationToken);
        return new HttpResponseMessage(statusCode);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseAll();
            _deliveries.Writer.TryComplete();
        }

        base.Dispose(disposing);
    }
}
