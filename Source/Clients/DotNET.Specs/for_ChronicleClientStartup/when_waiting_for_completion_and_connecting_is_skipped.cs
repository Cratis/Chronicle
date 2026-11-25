// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClientStartup;

public class when_waiting_for_completion_and_connecting_is_skipped : given.a_chronicle_client_startup
{
    bool _completed;

    void Establish() => _shouldConnect = false;

    async Task Because()
    {
        await StartAndSignalStarted();
        await _startup.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        _completed = _startup.Completion.IsCompletedSuccessfully;
    }

    [Fact] void should_complete() => _completed.ShouldBeTrue();
    [Fact] void should_not_connect() => _connection.DidNotReceive().Connect();
}
