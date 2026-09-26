// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class join_inside_nested_children : for_LanguageService.given.a_language_service
{
    CompilerErrors _errors;
    CompilerErrors _deepErrors;

    void Because()
    {
        var result = _languageService.Compile("""
            projection Test => CompanyReadModel
              children departments identified by id
                from DepartmentCreated
                nested details
                  from DepartmentCreated
                  join Department on id
                    with TestEvent
            """,
            ProjectionOwner.Client,
            [],
            []);
        _errors = result.Match(_ => CompilerErrors.Empty, errors => errors);
        var deepResult = _languageService.Compile("""
            projection Test => CompanyReadModel
              children departments identified by id
                from DepartmentCreated
                nested details
                  from DepartmentCreated
                  nested address
                    from DepartmentCreated
                    join Department on id
                      with TestEvent
            """,
            ProjectionOwner.Client,
            [],
            []);
        _deepErrors = deepResult.Match(_ => CompilerErrors.Empty, errors => errors);
    }

    [Fact] void should_reject_the_join_in_a_nested_child() => _errors.Errors.ShouldContain(_ => _.Message.Contains("'join' inside 'nested' under 'children' is not supported", StringComparison.Ordinal));
    [Fact] void should_reject_the_join_at_deeper_nested_levels() => _deepErrors.Errors.ShouldContain(_ => _.Message.Contains("'join' inside 'nested' under 'children' is not supported", StringComparison.Ordinal));
}
