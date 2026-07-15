// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilderExtensions;

public class when_using_with_tls_validation_skipped : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionStringBuilder _result;

    void Establish() => _builder = new ChronicleConnectionStringBuilder();

    void Because() => _result = _builder.WithTlsValidationSkipped();

    [Fact] void should_set_skip_tls_validation_to_true() => _builder.SkipTlsValidation.ShouldBeTrue();
    [Fact] void should_return_builder_for_fluent_chaining() => _result.ShouldEqual(_builder);
}
