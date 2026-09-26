// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_unsupported_block;

public class with_variant_inside_children_without_schemas : given.a_language_service_compiling_variants
{
    const string Declaration = """
        projection Test => CompanyReadModel
          children departments identified by id
            variant CompanyReadModel
              enters on DepartmentCreated
        """;

    void Because() => Compile(Declaration, withSchemas: false);

    [Fact] void should_report_the_nested_variant_at_its_location() => _errors.Errors.ShouldContain(error => error.Message.Contains("variant", StringComparison.OrdinalIgnoreCase) && error.Line == 3 && error.Column == 5);
}
