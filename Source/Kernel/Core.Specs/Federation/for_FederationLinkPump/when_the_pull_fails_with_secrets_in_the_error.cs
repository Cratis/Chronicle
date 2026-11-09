// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_the_pull_fails_with_secrets_in_the_error : given.a_link_pump
{
    const string PasswordPath = "/run/secrets/federation-client.pass";
    const string Pin = "sha256:q83vEjRWeJDvq83vEjRWeJDvq83vEjRWeJDvq83vEjQ=";

    TimeSpan? _delay;

    void Establish() =>
        _pull.Handler = _ => throw new InvalidOperationException($"TLS failed with {Pin} while reading {PasswordPath} token ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

    async Task Because() => _delay = await _pump.Step(default);

    [Fact] void should_be_in_backoff() => _status.GetBinding(BindingId).State.ShouldEqual(FederationBindingStates.Backoff);
    [Fact] void should_retry_after_the_initial_backoff() => _delay.ShouldEqual(_timing.InitialBackoff);
    [Fact] void should_record_an_error() => _status.GetBinding(BindingId).LastError.ShouldNotBeNull();
    [Fact] void should_not_leak_the_pin() => _status.GetBinding(BindingId).LastError.ShouldNotContain("q83vEjRW");
    [Fact] void should_not_leak_the_path() => _status.GetBinding(BindingId).LastError.ShouldNotContain("federation-client");
    [Fact] void should_not_leak_the_token() => _status.GetBinding(BindingId).LastError.ShouldNotContain("ABCDEFGHIJKLMNOP");
    [Fact] void should_close_the_connection_so_a_fresh_one_is_made() => _clients.Closed.ShouldEqual(1);
}
