// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilderExtensions;

public class when_using_without_authentication : Specification
{
    ChronicleConnectionStringBuilder _builder;
    AuthenticationMode _mode;

    void Establish() => _builder = new ChronicleConnectionStringBuilder();

    void Because() => _mode = _builder.WithHost("localhost").WithoutAuthentication().AuthenticationMode;

    [Fact] void should_present_no_credentials() => _mode.ShouldEqual(AuthenticationMode.None);
    [Fact] void should_record_it_on_the_builder() => _builder.NoAuthentication.ShouldBeTrue();
}
