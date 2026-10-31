// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_compensation_schemas;

public class and_the_unmarked_target_is_not_registered : given.registry_bindings
{
    Exception _preciseError;
    Exception _legacyError;
    Exception _declarationError;

    void Establish() => _first.GetEventTypeFor(typeof(PlainEvent)).Throws(new TypeIsNotAnEventType(typeof(PlainEvent)));

    void Because()
    {
        _preciseError = Catch.Exception(() => _generator.Generate(typeof(Compensation), _first));
        _legacyError = Catch.Exception(() => _generator.GenerateLegacyEventType(typeof(Compensation), _first));
        _declarationError = Catch.Exception(() => _generator.Generate(typeof(Compensation)));
    }

    [Fact] void should_require_a_registered_precise_target() => _preciseError.ShouldBeOfExactType<TypeIsNotAnEventType>();
    [Fact] void should_require_a_registered_legacy_target() => _legacyError.ShouldBeOfExactType<TypeIsNotAnEventType>();
    [Fact] void should_preserve_declaration_only_validation() => _declarationError.ShouldBeOfExactType<TypeIsNotAnEventType>();
}
