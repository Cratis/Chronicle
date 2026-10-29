// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceHttpClientFactory;

public class when_warn_mode_sends_to_an_unlisted_host : given.a_guarded_capture_client
{
    async Task Because()
    {
        using var client = await Factory.Create(_definition);
        using var response = await client.GetAsync(string.Empty);
    }

    [Fact] void should_send_the_request() => _transport.Targets.Count.ShouldEqual(1);
    [Fact] void should_log_a_warning_naming_the_capture_settings() => _warnings.ShouldContain(message => message.Contains("ExternalServices:AllowedHosts"));
}
