// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_event_type_naming_an_explicit_generation : given.a_language_service_with_event_type_generations
{
    const string Declaration = """
        projection TestProjection => TestModel
            from TestEvent
                key $eventSourceId
                name = value
        """;

    CompilerErrors _namingFirstGeneration;
    CompilerErrors _namingSecondGeneration;

    void Because()
    {
        var schemas = new[]
        {
            EventTypeSchemaFor(1, JsonObjectType.String),
            EventTypeSchemaFor(2, JsonObjectType.Integer)
        };

        _namingFirstGeneration = Validate(schemas, "TestEvent+1");
        _namingSecondGeneration = Validate(schemas, "TestEvent+2");
    }

    [Fact] void should_use_the_schema_of_the_first_generation_when_it_is_named() => _namingFirstGeneration.HasErrors.ShouldBeFalse();
    [Fact] void should_use_the_schema_of_the_second_generation_when_it_is_named() => _namingSecondGeneration.Errors.Any(_ => _.Message.Contains("Type mismatch")).ShouldBeTrue();

    CompilerErrors Validate(IEnumerable<EventTypeSchema> schemas, string eventReference)
    {
        // The declaration language has no syntax for naming a generation, so the reference is set on the parsed syntax.
        var parsed = new ScreenplayCompiler().CompileProjection(Declaration).Value!;
        var fromBlock = parsed.Blocks.OfType<FromSyntax>().Single();
        var syntax = parsed with { Blocks = [fromBlock with { Events = [fromBlock.Events.Single() with { Event = eventReference }] }] };

        var errors = new CompilerErrors();
        new ProjectionValidator([_readModelDefinition], schemas).Validate(syntax, errors);
        return errors;
    }
}
