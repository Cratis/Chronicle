// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MigrationGenerationEventTypeIdAnalyzer.when_analyzing_migration;

public class and_previous_generation_is_for_a_different_event_type : given.a_migration_generation_event_type_id_analyzer
{
    const string Usage = """
    [EventType("customer-registered", generation: 2)]
    public record CustomerRegisteredV2(string FirstName, string LastName);

    [EventType("other-event", generation: 1)]
    public record OtherEventV1(string Name);

    [EventTypeGenerationFor<OtherEventV1>(1)]
    public record CustomerRegisteredV1(string Name);

    public class {|#0:CustomerRegisteredMigration|}
        : Cratis.Chronicle.Events.Migrations.EventTypeMigration<CustomerRegisteredV2, CustomerRegisteredV1>;
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.MigrationGenerationEventTypeIdAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.MigrationGenerationEventTypeId, DiagnosticSeverity.Warning, "CustomerRegisteredMigration"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
