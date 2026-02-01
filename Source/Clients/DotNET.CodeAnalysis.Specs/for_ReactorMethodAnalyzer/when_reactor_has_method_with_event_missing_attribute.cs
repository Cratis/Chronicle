// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Analyzers;
using Xunit;
using static Cratis.Chronicle.CodeAnalysis.Specs.AnalyzerTestHelpers;

namespace Cratis.Chronicle.CodeAnalysis.Specs.for_ReactorMethodAnalyzer;

public class when_reactor_has_method_with_event_missing_attribute
{
    [Fact]
    public async Task should_report_diagnostic()
    {
        var code = @"
using Cratis.Chronicle.Reactors;

namespace TestNamespace
{
    public record OrderShipped(string OrderId);

    public class OrderReactor : IReactor
    {
        public async Task Shipped(OrderShipped @event)
        {
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<ReactorMethodAnalyzer>(code);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.ReactorEventParameterMustHaveAttribute, diagnostics[0].Id);
    }

    [Fact]
    public async Task should_not_report_diagnostic_when_event_has_attribute()
    {
        var code = @"
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Concepts.Events;

namespace TestNamespace
{
    [EventType]
    public record OrderShipped(string OrderId);

    public class OrderReactor : IReactor
    {
        public async Task Shipped(OrderShipped @event)
        {
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<ReactorMethodAnalyzer>(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task should_report_diagnostic_for_invalid_signature()
    {
        var code = @"
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Concepts.Events;

namespace TestNamespace
{
    [EventType]
    public record OrderShipped(string OrderId);

    public class OrderReactor : IReactor
    {
        public async Task Shipped(OrderShipped @event, EventContext context, string extra)
        {
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<ReactorMethodAnalyzer>(code);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.ReactorMethodSignatureMustMatchAllowed, diagnostics[0].Id);
    }
}
