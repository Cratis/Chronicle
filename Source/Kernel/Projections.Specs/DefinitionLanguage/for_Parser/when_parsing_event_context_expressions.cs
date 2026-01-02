// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_event_context_expressions : Specification
{
    const string definition = """
        projection Activity => ActivityReadModel
          from ActivityLogged
            key $eventSourceId
            Occurred = ctx.occurred
            SequenceNumber = ctx.sequenceNumber
            CorrelationId = ctx.correlationId
            EventSourceId = ctx.eventSourceId
        """;

    FromEventBlock _onEvent;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _onEvent = (FromEventBlock)result.Projections[0].Directives[0];
    }

    [Fact] void should_have_four_mappings() => _onEvent.Mappings.Count.ShouldEqual(4);
    [Fact] void should_have_event_source_id_key() => _onEvent.Key.ShouldBeOfExactType<EventSourceIdExpression>();
    [Fact] void should_have_occurred_context() => ((AssignmentOperation)_onEvent.Mappings[0]).Value.ShouldBeOfExactType<EventContextExpression>();
    [Fact] void should_have_sequence_number_context() => ((AssignmentOperation)_onEvent.Mappings[1]).Value.ShouldBeOfExactType<EventContextExpression>();
    [Fact] void should_have_correlation_id_context() => ((AssignmentOperation)_onEvent.Mappings[2]).Value.ShouldBeOfExactType<EventContextExpression>();
    [Fact] void should_have_event_source_id_context() => ((AssignmentOperation)_onEvent.Mappings[3]).Value.ShouldBeOfExactType<EventContextExpression>();
}
