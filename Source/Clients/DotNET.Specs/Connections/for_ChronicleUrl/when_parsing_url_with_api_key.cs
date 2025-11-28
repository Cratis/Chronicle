// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleUrl;

public class when_parsing_url_with_api_key : Specification
{
    ChronicleUrl _url;

    void Establish() => _url = new ChronicleUrl("chronicle://localhost:35000?auth=ApiKey&key=myapikey123");

    [Fact] void should_have_correct_host() => _url.ServerAddress.Host.ShouldEqual("localhost");
    [Fact] void should_have_correct_port() => _url.ServerAddress.Port.ShouldEqual(35000);
    [Fact] void should_have_api_key_auth_mode() => _url.AuthenticationMode.ShouldEqual(AuthenticationMode.ApiKey);
    [Fact] void should_have_api_key() => _url.ApiKey.ShouldEqual("myapikey123");
}
