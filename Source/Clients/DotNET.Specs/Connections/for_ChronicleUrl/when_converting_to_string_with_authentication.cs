// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleUrl;

public class when_converting_to_string_with_authentication : Specification
{
    ChronicleUrl _url;
    string _result;

    void Establish() => _url = new ChronicleUrl("chronicle://admin:secret@localhost:35000");

    void Because() => _result = _url.ToString();

    [Fact] void should_include_username_and_password() => _result.ShouldEqual("chronicle://admin:secret@localhost");
}
