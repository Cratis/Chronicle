// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MigrationGenerationEventTypeIdAnalyzer.when_analyzing_migration;

public class and_a_generation_has_no_explicit_id : given.a_migration_generation_event_type_id_analyzer
{
    const string Usage = """
    [EventType(generation: 1)]
    public record CustomerRegisteredV1(string Name);

    [EventType(generation: 2)]
    public record CustomerRegisteredV2(string FirstName, string LastName);

    public class {|#0:CustomerRegisteredMigration|}
        : Cratis.Chronicle.Events.Migrations.EventTypeMigration<CustomerRegisteredV2, CustomerRegisteredV1>;
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.MigrationGenerationEventTypeIdAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.MigrationGenerationEventTypeId, DiagnosticSeverity.Warning, "CustomerRegisteredMigration"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
