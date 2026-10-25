// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Receivers.for_MapChronicleReceiver;

public class when_the_client_aborts_the_request : given.a_receiver_host
{
    bool _callCancelled;
    protected override bool HoldDelivery => true;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        using var content = new ByteArrayContent(CreateRequestBody());
        await SendAndAbort(content, cancellation);
    }

    async Task SendAndAbort(HttpContent content, CancellationTokenSource cancellation)
    {
        var sending = _http.PostAsync(Path, content, cancellation.Token);
        await _received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cancellation.CancelAsync();
        try
        {
            using var response = await sending;
        }
        catch (OperationCanceledException)
        {
            _callCancelled = true;
        }

        await _aborted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_cancel_the_http_call() => _callCancelled.ShouldBeTrue();
    [Fact] void should_cancel_the_token_passed_to_the_receiver() => _cancellationToken.IsCancellationRequested.ShouldBeTrue();
}
