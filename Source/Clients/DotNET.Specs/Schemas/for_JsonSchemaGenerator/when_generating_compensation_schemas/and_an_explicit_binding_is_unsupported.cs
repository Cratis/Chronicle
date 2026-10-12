// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_compensation_schemas;

public class and_an_explicit_binding_is_unsupported : given.registry_bindings
{
    IJsonSchemaGenerator _custom;
    Exception _preciseError;
    Exception _legacyError;

    void Establish() => _custom = new CustomGenerator();

    void Because()
    {
        _preciseError = Catch.Exception(() => _custom.Generate(typeof(Compensation), _first));
        _legacyError = Catch.Exception(() => _custom.GenerateLegacyEventType(typeof(Compensation), _first));
    }

    [Fact] void should_refuse_only_the_new_precise_binding() => _preciseError.ShouldBeOfExactType<RegistryAwareEventTypeSchemasNotSupported>();
    [Fact] void should_refuse_only_the_new_legacy_binding() => _legacyError.ShouldBeOfExactType<RegistryAwareEventTypeSchemasNotSupported>();
}
