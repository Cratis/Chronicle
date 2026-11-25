// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeGenerationMigratorAnalyzer.when_analyzing_event_type;

public class and_old_style_generations_have_a_migrator : given.an_event_type_generation_migrator_analyzer
{
    const string Usage = """
    [EventType("Customer.Registered", generation: 1)]
    public record CustomerRegisteredV1(string Name);

    [EventType("Customer.Registered", generation: 2)]
    public record CustomerRegistered(string FirstName, string LastName);

    public class V1ToV2 : EventTypeMigration<CustomerRegistered, CustomerRegisteredV1>;
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeGenerationMigratorAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
