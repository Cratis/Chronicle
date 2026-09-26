// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class children_inside_nested : for_LanguageService.given.a_language_service
{
    CompilerErrors _errors;

    void Because() => _errors = _languageService.Compile(
        """
        projection Test => CompanyReadModel
          nested settings
            from DepartmentCreated
            children entries identified by id
              from EmployeeCreated
        """,
        ProjectionOwner.Client,
        [],
        []).Match(_ => CompilerErrors.Empty, errors => errors);

    [Fact] void should_reject_an_unwired_child_block() => _errors.Errors.ShouldContain(
        _ => _.Message.Contains("'children' inside 'nested' is not supported", StringComparison.Ordinal) && _.Line == 4);
}
