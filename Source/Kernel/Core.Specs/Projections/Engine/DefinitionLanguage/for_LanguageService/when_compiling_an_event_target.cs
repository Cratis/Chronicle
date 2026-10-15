// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_compiling_an_event_target : given.a_language_service_with_schemas<when_compiling_an_event_target.PublishedOrder>
{
    public record PublishedOrder(decimal Amount);

    const string Declaration = """
        projection Publisher => PublishedOrder
          from MoneyDeposited
            key $eventSourceId
            amount = amount
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.MoneyDeposited)];

    ProjectionDefinition _definition;
    string _generated;

    void Establish() => _readModelDefinition = _readModelDefinition with
    {
        Sink = new SinkDefinition(
            SinkConfigurationId.None,
            WellKnownSinkTypes.EventSequence,
            new EventSequenceSinkConfiguration(new EventType("PublishedOrder", EventTypeGeneration.First), null, true))
    };

    void Because()
    {
        _definition = _languageService.Compile(Declaration, ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(projection => projection, errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors)}"));
        _generated = _languageService.Generate(_definition, _readModelDefinition);
    }

    [Fact] void should_target_the_registered_event_target_by_name() => _definition.ReadModel.ShouldEqual(new ReadModelIdentifier("PublishedOrder"));
    [Fact] void should_consume_the_source_event() => _definition.From.Keys.ShouldContain((EventType)"MoneyDeposited");
    [Fact] void should_round_trip_the_target_name() => _generated.ShouldContain("=> PublishedOrder");
    [Fact] void should_keep_the_target_an_event_sequence_sink() => _readModelDefinition.Sink.EventSequence.ShouldNotBeNull();
}
