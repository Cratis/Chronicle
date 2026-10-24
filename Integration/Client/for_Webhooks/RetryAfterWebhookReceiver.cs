// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Integration.for_Webhooks;

/// <summary>
/// A receiver that applies back-pressure to its first request, then accepts delivery.
/// </summary>
/// <param name="requests">All HTTP requests, including the rejected request.</param>
/// <param name="accepted">Successfully delivered requests.</param>
public class RetryAfterWebhookReceiver(InvokedWebhooks requests, InvokedWebhooks accepted) : HttpMessageHandler
{
    readonly TaskCompletionSource _retryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseRetry = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _attempts;

    /// <summary>
    /// Waits for the retry HTTP request, whose response remains held for inspection.
    /// </summary>
    /// <returns>The retry arrival signal.</returns>
    public Task WaitForRetry() => _retryStarted.Task;

    /// <summary>
    /// Allows the held retry to succeed.
    /// </summary>
    public void ReleaseRetry() => _releaseRetry.TrySetResult();

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = await request.Content!.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(',', header.Value));
        requests.Add(body, headers);
        var attempt = Interlocked.Increment(ref _attempts);
        if (attempt == 1)
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new(TimeSpan.FromSeconds(60));
            return response;
        }

        if (attempt == 3)
        {
            _retryStarted.TrySetResult();
            await _releaseRetry.Task.WaitAsync(cancellationToken);
        }

        accepted.Add(body, headers);
        return new(HttpStatusCode.OK);
    }
}
