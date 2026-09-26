// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_unsupported_block;

public class with_variant_at_root_and_schemas : given.a_language_service_compiling_variants
{
    const string Declaration = """
        projection Test => CompanyReadModel
          variant CompanyReadModel
            enters on DepartmentCreated
        """;

    void Because() => Compile(Declaration, withSchemas: true);

    [Fact] void should_report_the_unsupported_variant_at_its_location() => _errors.Errors.ShouldContain(error => error.Message == "Projection block of type 'ProjectionVariantSyntax' is not supported" && error.Line == 2 && error.Column == 3);
}
