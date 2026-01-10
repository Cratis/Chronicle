// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Clients.Connections.for_ChronicleConnectionString;

public class when_converting_to_string_with_api_key : Specification
{
    ChronicleConnectionString _url;
    string _result;

    void Establish() => _url = new ChronicleConnectionString("chronicle://localhost?auth=ApiKey&key=testkey");

    void Because() => _result = _url.ToString();

    [Fact] void should_include_auth_and_key_in_query() => _result.ShouldEqual("chronicle://localhost:35000?auth=ApiKey&key=testkey");
}
