// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Clients.Connections.for_ChronicleConnectionString;

public class when_converting_to_string_with_authentication : Specification
{
    ChronicleConnectionString _url;
    string _result;

    void Establish() => _url = new ChronicleConnectionString("chronicle://admin:secret@localhost:35000");

    void Because() => _result = _url.ToString();

    [Fact] void should_include_username_and_password() => _result.ShouldEqual("chronicle://admin:secret@localhost:35000");
}
