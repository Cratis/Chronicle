// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

public class a_connection_with_compatibility_timeouts : a_connection_with_several_endpoints
{
    protected override bool SkipCompatibilityCheck => false;

    void Establish() => StallCompatibilityChecks();

    protected void StallCompatibilityChecks()
    {
        _compatibility[_a] = () => new TaskCompletionSource<CompatibilityResponse>().Task;
        _compatibility[_b] = () => new TaskCompletionSource<CompatibilityResponse>().Task;
    }

    protected async Task CompleteTimedOutAttempt(Task attempt)
    {
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(5));
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds - 5));
        await attempt.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }
}
