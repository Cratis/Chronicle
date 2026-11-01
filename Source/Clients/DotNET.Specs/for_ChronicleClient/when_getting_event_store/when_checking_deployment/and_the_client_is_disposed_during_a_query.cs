// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_checking_deployment;

public class and_the_client_is_disposed_during_a_query : given.a_serverless_client
{
    void Establish() => CreateClient();

    async Task Because()
    {
        await _client.GetEventStore("store", "one");
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _client.Dispose();
        await _cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _client.Dispose();
    }

    [Fact] void should_cancel_the_rpc() => _cancelled.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_log_cancellation_as_failure() => _logger.Finished.Task.IsCompleted.ShouldBeFalse();
}
