// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_work_outlives_the_connecting_request : given.a_lifecycle_with_ambient_state
{
    readonly TaskCompletionSource _requestEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _background;
    string _backgroundAmbient;
    Activity _backgroundActivity;

    void Establish() => _lifecycle.OnConnected += () =>
    {
        _background = Task.Run(async () =>
        {
            await _requestEnded.Task;
            _backgroundAmbient = _ambient.Value;
            _backgroundActivity = Activity.Current;
        });
        return Task.CompletedTask;
    };

    async Task Because()
    {
        SetCallerState();
        await _lifecycle.Connected();
        _ambient.Value = string.Empty;
        _activity.Stop();
        _requestEnded.SetResult();
        await _background.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_not_retain_the_ended_requests_ambient_state() => _backgroundAmbient.ShouldBeNull();
    [Fact] void should_not_retain_the_ended_requests_activity() => _backgroundActivity.ShouldBeNull();
}
