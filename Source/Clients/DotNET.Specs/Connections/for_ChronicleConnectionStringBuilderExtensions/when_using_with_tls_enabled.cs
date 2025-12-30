// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilderExtensions;

public class when_using_with_tls_enabled : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionStringBuilder _result;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            DisableTls = true
        };
    }

    void Because() => _result = _builder.WithTlsEnabled();

    [Fact] void should_set_disable_tls_to_false() => _builder.DisableTls.ShouldBeFalse();
    [Fact] void should_return_builder_for_fluent_chaining() => _result.ShouldEqual(_builder);
}
