// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilderExtensions;

public class when_skipping_keep_alive : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionStringBuilder _result;

    void Establish() => _builder = new ChronicleConnectionStringBuilder();

    void Because() => _result = _builder.WithKeepAliveSkipped();

    [Fact] void should_set_skip_keep_alive_to_true() => _builder.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_return_builder_for_fluent_chaining() => _result.ShouldEqual(_builder);
}
