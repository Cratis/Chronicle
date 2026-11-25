// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeGenerationMigratorAnalyzer.when_analyzing_event_type;

public class and_migrator_belongs_to_a_different_event_type : given.an_event_type_generation_migrator_analyzer
{
    const string Usage = """
    [EventType("person-registered", generation: 2)]
    public record {|#0:PersonRegistered|}(string FirstName, string LastName);

    [EventTypeGenerationFor<PersonRegistered>(1)]
    public record PersonRegisteredV1(string Name);

    [EventType("order-placed", generation: 2)]
    public record OrderPlaced(string Number);

    [EventTypeGenerationFor<OrderPlaced>(1)]
    public record OrderPlacedV1(string Id);

    public class OrderV1ToV2 : EventTypeMigration<OrderPlaced, OrderPlacedV1>;
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeGenerationMigratorAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeGenerationMustHaveMigrator, DiagnosticSeverity.Error, "PersonRegistered", "1->2"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
