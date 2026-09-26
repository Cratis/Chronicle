// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MigrationGenerationEventTypeIdAnalyzer.when_analyzing_migration;

public class and_three_generations_share_an_event_type : given.a_migration_generation_event_type_id_analyzer
{
    const string Usage = """
    [EventType("person-registered", generation: 3)]
    public record PersonRegistered(string Email, string FirstName, string LastName);

    [EventTypeGenerationFor<PersonRegistered>(2)]
    public record PersonRegisteredV2(string Email, string Name);

    [EventTypeGenerationFor<PersonRegistered>(1)]
    public record PersonRegisteredV1(string EmailAddress, string Name);

    public class V1ToV2 : EventTypeMigration<PersonRegisteredV2, PersonRegisteredV1>;
    public class V2ToV3 : EventTypeMigration<PersonRegistered, PersonRegisteredV2>;
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.MigrationGenerationEventTypeIdAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
