// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EncryptedOnEventSourceIdAnalyzer.when_analyzing_members;

public class and_encrypted_on_event_source_id_property : given.an_encrypted_on_event_source_id_analyzer
{
    const string Usage = """
    public record PartnerId(Guid Value) : EventSourceId<Guid>(Value);

    public class Partner
    {
        {|#0:[Encrypted]|}
        public PartnerId Id { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EncryptedOnEventSourceIdAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.EncryptedOnEventSourceId, DiagnosticSeverity.Error, "Id", "PartnerId"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
