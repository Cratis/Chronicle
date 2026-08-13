// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_PassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_read_model;

/// <summary>
/// A parent key identifies a child inside the document the replay already reaches.
/// </summary>
public class and_only_a_parent_key_is_declared : given.a_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record LineAdded(string OrderId, string Sku);

    [Passive]
    [FromEvent<LineAdded>(parentKey: "OrderId")]
    public record OrderLine(
        [Key] string Id,
        string Sku);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.PassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
