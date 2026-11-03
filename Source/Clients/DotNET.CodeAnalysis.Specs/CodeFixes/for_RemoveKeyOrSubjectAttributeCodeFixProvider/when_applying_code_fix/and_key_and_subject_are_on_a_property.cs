// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_RemoveKeyOrSubjectAttributeCodeFixProvider.when_applying_code_fix;

public class and_key_and_subject_are_on_a_property : given.a_remove_key_or_subject_attribute_code_fix_provider
{
    Task _result;

    void Because() => _result = CodeFixVerifier<CodeAnalysis.Analyzers.KeyOrSubjectOnEventSourceIdAnalyzer, CodeAnalysis.CodeFixes.RemoveKeyOrSubjectAttributeCodeFixProvider>.VerifyCodeFix(
        CreateSource("""
            public record ActorId(Guid Value) : EventSourceId<Guid>(Value);
            public record OrderPlaced([property: {|#0:Key|}, Subject] ActorId Actor);
            """),
        CreateSource("""
            public record ActorId(Guid Value) : EventSourceId<Guid>(Value);
            public record OrderPlaced([property: Subject] ActorId Actor);
            """),
        new ExpectedDiagnostic(DiagnosticIds.KeyOrSubjectOnEventSourceId, DiagnosticSeverity.Warning, "Actor", "ActorId", "Key"));

    [Fact] Task should_remove_only_the_redundant_key() => _result;
}
