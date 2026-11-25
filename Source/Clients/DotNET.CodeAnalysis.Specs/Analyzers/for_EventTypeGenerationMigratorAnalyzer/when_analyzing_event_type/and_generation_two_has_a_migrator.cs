// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeGenerationMigratorAnalyzer.when_analyzing_event_type;

public class and_generation_two_has_a_migrator : given.an_event_type_generation_migrator_analyzer
{
    const string Usage = """
    [EventType("person-registered", generation: 2)]
    public record PersonRegistered(string FirstName, string LastName);

    [EventTypeGenerationFor<PersonRegistered>(1)]
    public record PersonRegisteredV1(string Name);

    public class V1ToV2 : EventTypeMigration<PersonRegistered, PersonRegisteredV1>;
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeGenerationMigratorAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
