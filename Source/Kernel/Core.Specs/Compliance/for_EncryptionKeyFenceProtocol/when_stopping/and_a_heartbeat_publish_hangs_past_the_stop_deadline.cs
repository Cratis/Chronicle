// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyFenceProtocol.when_stopping;

public class and_a_heartbeat_publish_hangs_past_the_stop_deadline : given.a_started_protocol_with_a_hanging_publish
{
    bool _stopped;

    async Task Because()
    {
        var stopping = _observer.OnStop(_stopToken.Token);
        await _deadline.CancelAsync();
        try
        {
            await stopping.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _stopped = true;
        }
        catch (TimeoutException)
        {
        }
    }

    [Fact] void should_stop_at_the_stop_deadline_before_the_host_token() => _stopped.ShouldBeTrue();
}
