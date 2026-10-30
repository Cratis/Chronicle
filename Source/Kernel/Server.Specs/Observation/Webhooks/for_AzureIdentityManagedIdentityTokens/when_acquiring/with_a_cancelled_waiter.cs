// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_AzureIdentityManagedIdentityTokens.when_acquiring;

public class with_a_cancelled_waiter : given.a_token_provider
{
    Exception? _error;

    void Establish() => _credential.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var cancelled = _provider.AcquireToken(_authorization, cancellation.Token);
        var other = _provider.AcquireToken(_authorization, CancellationToken.None);
        await cancellation.CancelAsync();
        _error = await Catch.Exception(async () => await cancelled);
        _credential.Release!.SetResult();
        _result = await other;
    }

    [Fact] void should_cancel_only_the_waiter() => _error.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_not_cancel_the_shared_request() => _result.AccessToken.ShouldEqual("token-1");
    [Fact] void should_fetch_once() => _credential.Requests.ShouldEqual(1);
}
