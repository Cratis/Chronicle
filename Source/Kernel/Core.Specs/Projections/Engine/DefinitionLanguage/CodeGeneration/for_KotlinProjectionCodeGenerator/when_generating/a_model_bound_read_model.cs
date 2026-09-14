// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.Kotlin;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_KotlinProjectionCodeGenerator.when_generating;

public class a_model_bound_read_model : given.a_projection_to_generate
{
    KotlinProjectionCodeGenerator _generator = null!;
    Exception _exception = null!;

    void Establish() => _generator = new KotlinProjectionCodeGenerator();

    void Because() => _exception = Catch.Exception(() => _generator.GenerateModelBound(_definition, _readModelDefinition));

    [Fact] void should_say_the_client_has_no_api_for_it() =>
        _exception.ShouldBeOfExactType<ProjectionCodeGenerationNotSupported>();

    [Fact] void should_not_claim_to_support_it() =>
        _generator.Supports(ProjectionCodeStyle.ModelBound).ShouldBeFalse();

    [Fact] void should_still_support_the_declarative_form() =>
        _generator.Supports(ProjectionCodeStyle.Declarative).ShouldBeTrue();

    [Fact] void should_be_the_kotlin_generator() => _generator.Language.ShouldEqual(ProjectionCodeLanguage.Kotlin);
}
