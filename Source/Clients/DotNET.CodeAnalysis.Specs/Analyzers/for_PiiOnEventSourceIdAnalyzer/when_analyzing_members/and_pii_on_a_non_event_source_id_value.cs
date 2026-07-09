// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_PiiOnEventSourceIdAnalyzer.when_analyzing_members;

public class and_pii_on_a_non_event_source_id_value : given.a_pii_on_event_source_id_analyzer
{
    const string Usage = """
    public record CustomerRegistered(
        [PII] string SocialSecurityNumber,
        string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.PiiOnEventSourceIdAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
