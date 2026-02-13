// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilderExtensions;

public class when_using_with_credentials : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionStringBuilder _result;

    void Establish() => _builder = new ChronicleConnectionStringBuilder();

    void Because() => _result = _builder.WithCredentials("myUsername", "myPassword");

    [Fact] void should_set_username() => _builder.Username.ShouldEqual("myUsername");
    [Fact] void should_set_password() => _builder.Password.ShouldEqual("myPassword");
    [Fact] void should_return_builder_for_fluent_chaining() => _result.ShouldEqual(_builder);
}
