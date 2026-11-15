// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder.when_round_tripping_tls_validation;

public class with_an_omitted_setting : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionStringBuilder _clone;
    ChronicleConnectionString _result;
    ChronicleConnectionString _withCredentials;
    ChronicleConnectionString _withApiKey;

    void Establish() => _builder = new("chronicle://server.example:35000");

    void Because()
    {
        _clone = new(_builder);
        _result = new(_clone.Build());
        _withCredentials = new(_result.WithCredentials("client", "secret").ToString());
        _withApiKey = new(_result.WithApiKey("key").ToString());
    }

    [Fact] void should_preserve_the_release_default() => _result.SkipTlsValidation.ShouldBeTrue();
    [Fact] void should_leave_the_clone_setting_omitted() => _clone.HasExplicitSkipTlsValidation.ShouldBeFalse();
    [Fact] void should_leave_the_reparsed_setting_omitted() => _result.HasExplicitSkipTlsValidation.ShouldBeFalse();
    [Fact] void should_omit_the_query_option() => _result.ToString().ShouldEqual("chronicle://server.example:35000");
    [Fact] void should_leave_the_setting_omitted_with_credentials() => _withCredentials.HasExplicitSkipTlsValidation.ShouldBeFalse();
    [Fact] void should_leave_the_setting_omitted_with_an_api_key() => _withApiKey.HasExplicitSkipTlsValidation.ShouldBeFalse();
}
