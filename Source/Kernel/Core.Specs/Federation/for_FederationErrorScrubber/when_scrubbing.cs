// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Federation.for_FederationErrorScrubber;

public class when_scrubbing : Specification
{
    string _withPin;
    string _withPath;
    string _withToken;
    string _withConfiguredSecret;
    string _fromRpc;
    string _nested;
    string _long;

    void Because()
    {
        _withPin = FederationErrorScrubber.Scrub(new InvalidOperationException("pin sha256:q83vEjRWeJDvq83vEjRWeJDvq83vEjRWeJDvq83vEjQ= mismatch"));
        _withPath = FederationErrorScrubber.Scrub(new FileNotFoundException("Could not find /etc/chronicle/federation/client.pfx"));
        _withToken = FederationErrorScrubber.Scrub(new InvalidOperationException("bad key MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA"));
        _withConfiguredSecret = FederationErrorScrubber.Scrub(new InvalidOperationException("password hunter2 rejected"), ["hunter2"]);
        _fromRpc = FederationErrorScrubber.Scrub(new RpcException(new Status(StatusCode.Unavailable, "internal host detail 10.0.0.7")));
        _nested = FederationErrorScrubber.Scrub(new HttpRequestException("outer", new System.Security.Authentication.AuthenticationException("rejected by callback")));
        _long = FederationErrorScrubber.ScrubText(new string('x', 1000) + " tail");
    }

    [Fact] void should_remove_a_pin() => _withPin.ShouldNotContain("q83vEjRW");
    [Fact] void should_remove_a_file_path() => _withPath.ShouldNotContain("client.pfx");
    [Fact] void should_remove_a_key_like_token() => _withToken.ShouldNotContain("MIIBIjANBg");
    [Fact] void should_remove_a_configured_secret() => _withConfiguredSecret.ShouldNotContain("hunter2");
    [Fact] void should_keep_the_exception_type() => _withPin.StartsWith("InvalidOperationException:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_only_the_status_code_of_a_remote_failure() => _fromRpc.ShouldEqual("gRPC Unavailable");
    [Fact] void should_name_the_innermost_cause() => _nested.ShouldContain("AuthenticationException");
    [Fact] void should_limit_the_length() => (_long.Length <= 200).ShouldBeTrue();
}
