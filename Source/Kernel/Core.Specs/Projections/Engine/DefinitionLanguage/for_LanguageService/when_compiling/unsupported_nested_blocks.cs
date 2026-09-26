// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class unsupported_nested_blocks : for_LanguageService.given.a_language_service
{
    CompilerErrors _removal;

    void Because()
    {
        _removal = Errors("""
            projection Test => CompanyReadModel
              nested settings
                from DepartmentCreated
                remove via join on DepartmentDeleted
            """);
    }

    CompilerErrors Errors(string declaration) => _languageService.Compile(declaration, ProjectionOwner.Client, [], [])
        .Match(_ => CompilerErrors.Empty, errors => errors);

    [Fact] void should_reject_nested_removal_via_join() => _removal.Errors.ShouldContain(_ => _.Message.Contains("'remove via join' inside 'nested' is not supported", StringComparison.Ordinal));
}
