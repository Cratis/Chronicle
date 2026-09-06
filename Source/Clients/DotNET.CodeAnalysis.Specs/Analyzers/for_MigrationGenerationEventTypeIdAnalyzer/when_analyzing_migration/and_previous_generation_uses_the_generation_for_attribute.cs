// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MigrationGenerationEventTypeIdAnalyzer.when_analyzing_migration;

public class and_previous_generation_uses_the_generation_for_attribute : given.a_migration_generation_event_type_id_analyzer
{
    const string Usage = """
    [EventType("customer-registered", generation: 2)]
    public record CustomerRegisteredV2(string FirstName, string LastName);

    [EventTypeGenerationFor<CustomerRegisteredV2>(1)]
    public record CustomerRegisteredV1(string Name);

    public class CustomerRegisteredMigration
        : Cratis.Chronicle.Events.Migrations.EventTypeMigration<CustomerRegisteredV2, CustomerRegisteredV1>;
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.MigrationGenerationEventTypeIdAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
