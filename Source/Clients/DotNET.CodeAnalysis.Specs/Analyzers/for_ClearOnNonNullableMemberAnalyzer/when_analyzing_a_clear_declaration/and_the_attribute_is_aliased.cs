// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ClearOnNonNullableMemberAnalyzer.when_analyzing_a_clear_declaration;

public class and_the_attribute_is_aliased : given.a_clear_on_non_nullable_member_analyzer
{
    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ClearOnNonNullableMemberAnalyzer>.VerifyAnalyzer(
        "using Reset = Cratis.Chronicle.Projections.ModelBound.ClearWithAttribute<Sample.Cleared>;\n" + CreateSource("""
        public record Cleared;
        public class State
        {
            [{|#0:Reset|}] public string Name { get; set; } = string.Empty;
        }
        """),
        new ExpectedDiagnostic(DiagnosticIds.ClearOnNonNullableMember, DiagnosticSeverity.Warning, "Name", "string"));

    [Fact] Task should_preserve_the_diagnostic_for_a_closed_generic_alias() => _result;
}
