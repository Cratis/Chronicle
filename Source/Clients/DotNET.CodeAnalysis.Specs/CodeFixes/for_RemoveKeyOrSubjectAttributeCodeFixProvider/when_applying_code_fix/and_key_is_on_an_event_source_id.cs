// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_RemoveKeyOrSubjectAttributeCodeFixProvider.when_applying_code_fix;

public class and_key_is_on_an_event_source_id : given.a_remove_key_or_subject_attribute_code_fix_provider
{
    const string Usage = """
    public record AccountId(Guid Value) : EventSourceId<Guid>(Value);

    public record Account({|#0:[Key]|} AccountId Id, string Name);
    """;

    const string FixedUsage = """
    public record AccountId(Guid Value) : EventSourceId<Guid>(Value);

    public record Account(AccountId Id, string Name);
    """;

    Task _result;

    void Because() => _result = CodeFixVerifier<CodeAnalysis.Analyzers.KeyOrSubjectOnEventSourceIdAnalyzer, CodeAnalysis.CodeFixes.RemoveKeyOrSubjectAttributeCodeFixProvider>.VerifyCodeFix(
        CreateSource(Usage),
        CreateSource(FixedUsage),
        new ExpectedDiagnostic(DiagnosticIds.KeyOrSubjectOnEventSourceId, DiagnosticSeverity.Warning, "Id", "AccountId", "Key"));

    [Fact] Task should_remove_the_redundant_attribute() => _result;
}
