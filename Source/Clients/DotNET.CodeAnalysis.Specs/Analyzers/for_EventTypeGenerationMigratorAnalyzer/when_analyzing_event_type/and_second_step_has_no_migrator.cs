// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeGenerationMigratorAnalyzer.when_analyzing_event_type;

public class and_second_step_has_no_migrator : given.an_event_type_generation_migrator_analyzer
{
    const string Usage = """
    [EventType("person-registered", generation: 3)]
    public record {|#0:PersonRegistered|}(string Email, string FirstName, string LastName);

    [EventTypeGenerationFor<PersonRegistered>(2)]
    public record PersonRegisteredV2(string Email, string Name);

    [EventTypeGenerationFor<PersonRegistered>(1)]
    public record PersonRegisteredV1(string EmailAddress, string Name);

    public class V1ToV2 : EventTypeMigration<PersonRegisteredV2, PersonRegisteredV1>;
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeGenerationMigratorAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeGenerationMustHaveMigrator, DiagnosticSeverity.Error, "PersonRegistered", "person-registered", "3", "2->3", "EventTypeMigration<PersonRegistered, PersonRegisteredV2>"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
