// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_compiling_malformed_arithmetic_operands : given.a_language_service_with_schemas<given.UserReadModel>
{
    CompilerErrors _errors;

    void Because() => _errors = CompileExpectingErrors("""
        projection User => UserReadModel
          from UserCreated
            add age by 1e--3
            subtract version by --1
        """);

    [Fact] void should_reject_the_malformed_exponent() => _errors.Errors.Any(error => error.Line == 3).ShouldBeTrue();
    [Fact] void should_reject_the_malformed_negative_number() => _errors.Errors.Any(error => error.Line == 4).ShouldBeTrue();
}
