// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_PIIAndEncryptedCombinedAnalyzer.when_analyzing_members;

public class and_only_pii_is_present : given.a_pii_and_encrypted_combined_analyzer
{
    const string Usage = """
    public record CustomerRegistered([PII] string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.PIIAndEncryptedCombinedAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
