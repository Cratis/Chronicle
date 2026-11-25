// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeGenerationMigratorAnalyzer.when_analyzing_event_type;

public class and_no_steps_have_migrators : given.an_event_type_generation_migrator_analyzer
{
    const string Usage = """
    [EventType("person-registered", generation: 3)]
    public record {|#0:PersonRegistered|}(string Email, string FirstName, string LastName);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeGenerationMigratorAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeGenerationMustHaveMigrator, DiagnosticSeverity.Error, "PersonRegistered", "3", "1->2, 2->3", "declare record 'PersonRegisteredV2'", "declare record 'PersonRegisteredV1'"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
