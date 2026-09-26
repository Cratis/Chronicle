// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MigrationGenerationEventTypeIdAnalyzer.when_analyzing_migration;

public class and_generations_resolve_to_different_event_types : given.a_migration_generation_event_type_id_analyzer
{
    const string Usage = """
    [EventType("person-registered", generation: 3)]
    public record PersonRegistered(string Email, string FirstName, string LastName);

    [EventTypeGenerationFor<PersonRegistered>(2)]
    public record PersonRegisteredV2(string Email, string Name);

    [EventType("different-event", generation: 3)]
    public record DifferentEvent(string Name);

    [EventTypeGenerationFor<DifferentEvent>(1)]
    public record PersonRegisteredV1(string EmailAddress, string Name);

    public class {|#0:V1ToV2|} : EventTypeMigration<PersonRegisteredV2, PersonRegisteredV1>;
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.MigrationGenerationEventTypeIdAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.MigrationGenerationEventTypeId, DiagnosticSeverity.Warning, "V1ToV2"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
