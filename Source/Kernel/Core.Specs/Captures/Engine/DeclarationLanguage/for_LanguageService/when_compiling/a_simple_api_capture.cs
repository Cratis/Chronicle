// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_simple_api_capture : for_LanguageService.given.a_language_service
{
    const string Declaration = """
        capture Customers
          source api
            api CustomersApi
            poll 5m
            auth bearer $env.CustomersApiToken
          key customerId
          append CustomerChanged
            when email
            email = $.email
        """;

    CaptureDefinition _result;

    void Because() => _result = Compile(Declaration);

    [Fact] void should_have_api_source() => _result.Source.Type.ShouldEqual(SourceType.Api);
    [Fact] void should_have_api() => _result.Source.Api.ShouldEqual("CustomersApi");
    [Fact] void should_have_poll() => _result.Source.Poll.ShouldEqual("5m");
    [Fact] void should_have_auth() => _result.Source.Auth.ShouldEqual("bearer $env.CustomersApiToken");
    [Fact] void should_have_key_property() => _result.KeyProperty.ShouldEqual("customerId");
    [Fact] void should_have_append_event() => _result.Appends[0].EventType.ShouldEqual("CustomerChanged");
}
