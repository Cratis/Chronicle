// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_projection;

/// <summary>
/// A parent key inside a child block identifies the child within the document the replay already reaches.
/// </summary>
public class and_only_a_child_key_is_redirected : given.a_fluent_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record LineAdded(string OrderId, string Sku);

    public record OrderLine(string Sku);

    [Passive]
    public record Order(
        [Key] string Id,
        IEnumerable<OrderLine> Lines);

    public class OrderProjection : IProjectionFor<Order>
    {
        public void Define(IProjectionBuilderFor<Order> builder) => builder
            .Children(m => m.Lines, c => c
                .From<LineAdded>(_ => _
                    .UsingParentKey(e => e.OrderId)
                    .Set(m => m.Sku).To(e => e.Sku)));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
