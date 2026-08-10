// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// Contact is only a phantom generic argument: Recursive&lt;T&gt; serializes another recursive node but no T-valued member.
/// </summary>
public class and_a_recursive_generic_argument_is_not_serialized : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    [PII]
    public record Contact(string Email);

    public record Recursive<T>(Recursive<List<T>>? Next);

    public record AdvisorContactsChanged(string RequestId, Recursive<Contact> Details);

    [FromEvent<AdvisorContactsChanged>(key: "RequestId")]
    public record RequestSummary(
        [Key] string Id,
        Recursive<Contact> Details);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
