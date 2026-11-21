// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.for_ChronicleStartup;

public class when_use_is_not_called : given.a_registered_web_host
{
    async Task Because()
    {
        await SignalStarted();
        await _startedSignal.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_connect_once() => _connection.Received(1).Connect();
    [Fact] void should_warn_that_use_is_missing() => _logs.Entries.Count(entry => entry.Message.Contains("UseCratisChronicle() was not called")).ShouldEqual(1);
    [Fact] void should_keep_the_header_namespace_warning() => _logs.Entries.Count(entry => entry.Message.Contains("HTTP header")).ShouldEqual(1);
}
