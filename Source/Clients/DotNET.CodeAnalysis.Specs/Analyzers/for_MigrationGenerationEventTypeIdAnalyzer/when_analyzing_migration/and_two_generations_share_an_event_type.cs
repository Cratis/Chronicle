// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MigrationGenerationEventTypeIdAnalyzer.when_analyzing_migration;

public class and_two_generations_share_an_event_type : given.a_migration_generation_event_type_id_analyzer
{
    const string Usage = """
    [EventType(generation: 2)]
    public record PersonRegistered(string Email, string Name);

    [EventTypeGenerationFor<PersonRegistered>(1)]
    public record PersonRegisteredV1(string EmailAddress, string Name);

    public class V1ToV2 : EventTypeMigration<PersonRegistered, PersonRegisteredV1>;
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.MigrationGenerationEventTypeIdAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
