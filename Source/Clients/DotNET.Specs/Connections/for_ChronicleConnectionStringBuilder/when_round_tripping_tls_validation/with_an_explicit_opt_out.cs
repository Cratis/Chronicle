// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder.when_round_tripping_tls_validation;

public class with_an_explicit_opt_out : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionStringBuilder _clone;
    ChronicleConnectionString _result;
    ChronicleConnectionString _withCredentials;
    ChronicleConnectionString _withApiKey;

    void Establish() => _builder = new("chronicle://server.example:35000?skipTlsValidation=true");

    void Because()
    {
        _clone = new(_builder);
        _result = new(_clone.Build());
        _withCredentials = new(_result.WithCredentials("client", "secret").ToString());
        _withApiKey = new(_result.WithApiKey("key").ToString());
    }

    [Fact] void should_preserve_the_opt_out() => _result.SkipTlsValidation.ShouldBeTrue();
    [Fact] void should_keep_the_clone_setting_explicit() => _clone.HasExplicitSkipTlsValidation.ShouldBeTrue();
    [Fact] void should_keep_the_reparsed_setting_explicit() => _result.HasExplicitSkipTlsValidation.ShouldBeTrue();
    [Fact] void should_render_the_explicit_option() => _result.ToString().ShouldEqual("chronicle://server.example:35000?skipTlsValidation=true");
    [Fact] void should_keep_the_setting_explicit_with_credentials() => _withCredentials.HasExplicitSkipTlsValidation.ShouldBeTrue();
    [Fact] void should_preserve_the_opt_out_with_credentials() => _withCredentials.SkipTlsValidation.ShouldBeTrue();
    [Fact] void should_keep_the_setting_explicit_with_an_api_key() => _withApiKey.HasExplicitSkipTlsValidation.ShouldBeTrue();
    [Fact] void should_preserve_the_opt_out_with_an_api_key() => _withApiKey.SkipTlsValidation.ShouldBeTrue();
}
