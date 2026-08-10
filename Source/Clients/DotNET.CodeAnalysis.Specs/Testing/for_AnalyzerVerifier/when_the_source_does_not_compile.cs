// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Analyzers;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Testing.for_AnalyzerVerifier;

/// <summary>
/// The guard every negative spec in this project rests on. An analyzer reports nothing over source that does
/// not bind, so without this the snippet below — which names a type that does not exist — would satisfy
/// "should not report any diagnostic" while measuring nothing.
/// </summary>
public class when_the_source_does_not_compile : Specification
{
    const string Source = """
    namespace Sample
    {
        public record RequestSummary(Guid Id, Whoops Misspelled);
    }
    """;

    Exception _result;

    void Because() => _result = Catch.Exception(() => AnalyzerVerifier<EventTypeRecordAnalyzer>.VerifyAnalyzer(Source).GetAwaiter().GetResult());

    [Fact] void should_fail_rather_than_report_no_diagnostics() => _result.ShouldBeOfExactType<SpecSourceDoesNotCompile>();
    [Fact] void should_name_the_compiler_error() => _result.Message.ShouldContain("Whoops");
}
