// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClientStartup;

public class when_waiting_for_completion_after_connecting_fails : given.a_chronicle_client_startup
{
    bool _completed;

    async Task Because()
    {
        _connection.Connect().Returns(Task.FromException(new InvalidOperationException("unreachable")));
        await StartAndSignalStarted();
        await _startup.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        _completed = _startup.Completion.IsCompletedSuccessfully;
    }

    [Fact] void should_complete_without_faulting() => _completed.ShouldBeTrue();
}
