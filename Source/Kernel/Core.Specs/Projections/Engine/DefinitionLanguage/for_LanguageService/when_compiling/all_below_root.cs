// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class all_below_root : for_LanguageService.given.a_language_service_with_schemas<for_LanguageService.given.CompanyReadModel>
{
    CompilerErrors _childWithSchemas;
    CompilerErrors _childWithoutSchemas;
    CompilerErrors _nestedWithSchemas;
    CompilerErrors _nestedWithoutSchemas;

    protected override IEnumerable<Type> EventTypes => [typeof(for_LanguageService.given.DepartmentCreated)];

    void Because()
    {
        const string child = """
            projection Test => CompanyReadModel
              children departments identified by id
                from DepartmentCreated
                all
                  id = $eventSourceId
            """;
        const string nested = """
            projection Test => CompanyReadModel
              nested settings
                from DepartmentCreated
                all
                  id = $eventSourceId
            """;
        _childWithSchemas = Errors(child, true);
        _childWithoutSchemas = Errors(child, false);
        _nestedWithSchemas = Errors(nested, true);
        _nestedWithoutSchemas = Errors(nested, false);
    }

    CompilerErrors Errors(string declaration, bool schemas) => _languageService.Compile(
        declaration,
        ProjectionOwner.Client,
        schemas ? [_readModelDefinition] : [],
        schemas ? _eventTypeSchemas : []).Match(_ => CompilerErrors.Empty, errors => errors);

    [Fact] void should_reject_child_all_with_schemas() => AssertAllError(_childWithSchemas, 4);
    [Fact] void should_reject_child_all_without_schemas() => AssertAllError(_childWithoutSchemas, 4);
    [Fact] void should_reject_nested_all_with_schemas() => AssertAllError(_nestedWithSchemas, 4);
    [Fact] void should_reject_nested_all_without_schemas() => AssertAllError(_nestedWithoutSchemas, 4);

    static void AssertAllError(CompilerErrors errors, int line) => errors.Errors.ShouldContain(
        _ => _.Message.Contains("'all' block is only supported at the root", StringComparison.Ordinal) && _.Line == line);
}
