// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_event_type_only_at_a_later_generation : given.a_language_service_with_event_type_generations
{
    const string Declaration = """
        projection TestProjection => TestModel
            from TestEvent
                key $eventSourceId
                name = value
        """;

    CompilerErrors _matchingErrors;
    CompilerErrors _mismatchingErrors;

    void Because()
    {
        _matchingErrors = Compile(Declaration, EventTypeSchemaFor(2, JsonObjectType.String));
        _mismatchingErrors = Compile(Declaration, EventTypeSchemaFor(2, JsonObjectType.Integer));
    }

    [Fact] void should_find_the_event_type_when_declared_by_name() => _matchingErrors.HasErrors.ShouldBeFalse();
    [Fact] void should_not_report_the_event_type_as_missing() => _matchingErrors.Errors.Any(_ => _.Message.Contains("not found")).ShouldBeFalse();
    [Fact] void should_validate_against_the_schema_of_the_later_generation() => _mismatchingErrors.Errors.Any(_ => _.Message.Contains("Type mismatch")).ShouldBeTrue();
}
