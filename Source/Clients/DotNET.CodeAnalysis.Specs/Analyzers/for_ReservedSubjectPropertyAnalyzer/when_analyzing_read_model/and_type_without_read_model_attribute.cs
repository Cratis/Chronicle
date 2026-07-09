// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReservedSubjectPropertyAnalyzer.when_analyzing_read_model;

public class and_type_without_read_model_attribute : given.a_reserved_subject_property_analyzer
{
    const string Usage = """
    public record Customer(
        Guid Id,
        string _subject);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReservedSubjectPropertyAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
