// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleUrl;

public class when_converting_to_string_without_authentication : Specification
{
    ChronicleUrl _url;
    string _result;

    void Establish() => _url = new ChronicleUrl("chronicle://localhost:35000");

    void Because() => _result = _url.ToString();

    [Fact] void should_not_include_authentication() => _result.ShouldEqual("chronicle://localhost");
}
