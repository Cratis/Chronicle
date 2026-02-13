// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Clients.Connections.for_ChronicleConnectionString;

public class when_converting_to_string_without_authentication : Specification
{
    ChronicleConnectionString _url;
    string _result;

    void Establish() => _url = new ChronicleConnectionString("chronicle://localhost:35000");

    void Because() => _result = _url.ToString();

    [Fact] void should_not_include_authentication() => _result.ShouldEqual("chronicle://localhost:35000");
}
