// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Services.given;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.when_registering;

public class and_access_is_withdrawn : given.a_registration
{
    readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _unsubscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override IReducerMediator CreateMediator() => new ReducerMediator();

    void Establish()
    {
        _client.SetDefinitionAndSubscribe(Arg.Any<ReducerDefinition>()).Returns(_ =>
        {
            _subscribed.TrySetResult();
            return Task.CompletedTask;
        });
        _client.Unsubscribe().Returns(_ =>
        {
            _unsubscribed.TrySetResult();
            return Task.CompletedTask;
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_end_and_release_the_real_stream(bool expireLease)
    {
        using var caller = new an_entitled_caller();
        Connect(callContext: caller.Context);
        await _subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        if (expireLease) caller.Clock.Advance(TimeSpan.FromMinutes(6));
        else caller.Revoke();
        var error = await _error.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        ((RpcException)error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        await _unsubscribed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _messages.HasObservers.ShouldBeFalse();
        _lease.Received(1).Dispose();
    }

    [Fact]
    public async Task should_report_an_initial_refusal_to_the_subscriber()
    {
        using var caller = new an_entitled_caller();
        caller.Revoke();
        Connect(callContext: caller.Context);
        var error = await _error.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        ((RpcException)error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        await _client.DidNotReceive().SetDefinitionAndSubscribe(Arg.Any<ReducerDefinition>());
        _messages.HasObservers.ShouldBeFalse();
    }

    [Fact]
    public async Task should_release_a_call_cancelled_before_registration()
    {
        using var cancellation = new CancellationTokenSource();
        using var caller = new an_entitled_caller(cancellationToken: cancellation.Token);
        await cancellation.CancelAsync();
        Connect(callContext: caller.Context);
        _messages.HasObservers.ShouldBeFalse();
        _error.Task.IsCompleted.ShouldBeFalse();
        await _client.DidNotReceive().SetDefinitionAndSubscribe(Arg.Any<ReducerDefinition>());
    }

    [Fact]
    public async Task should_leave_an_audit_only_stream_connected()
    {
        using var caller = new an_entitled_caller(enforce: false);
        Connect(callContext: caller.Context);
        await _subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        caller.Revoke();
        caller.Clock.Advance(TimeSpan.FromMinutes(6));
        _error.Task.IsCompleted.ShouldBeFalse();
        _messages.HasObservers.ShouldBeTrue();
    }
}
