// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClientStartup;

public class when_the_host_connects_on_its_own : given.a_chronicle_client_startup
{
    TaskCompletionSource _decided;

    void Establish()
    {
        _shouldConnect = false;
        _decided = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _startup = new(_options, _client, _environment, _lifetime, _logger, () =>
        {
            _decided.TrySetResult();
            return _shouldConnect;
        });
    }

    async Task Because()
    {
        await StartAndSignalStarted();
        await _decided.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_not_connect() => _connection.DidNotReceive().Connect();
}
