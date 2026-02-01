// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Analyzers;
using Xunit;
using static Cratis.Chronicle.CodeAnalysis.Specs.AnalyzerTestHelpers;

namespace Cratis.Chronicle.CodeAnalysis.Specs.for_ReducerMethodAnalyzer;

public class when_reducer_has_method_with_event_missing_attribute
{
    [Fact]
    public async Task should_report_diagnostic()
    {
        var code = @"
using Cratis.Chronicle.Reducers;

namespace TestNamespace
{
    public record ItemAdded(string Name);
    
    public class CartState
    {
        public int ItemCount { get; set; }
    }

    public class CartReducer : IReducerFor<CartState>
    {
        public void Added(ItemAdded @event)
        {
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<ReducerMethodAnalyzer>(code);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.ReducerEventParameterMustHaveAttribute, diagnostics[0].Id);
    }

    [Fact]
    public async Task should_not_report_diagnostic_when_event_has_attribute()
    {
        var code = @"
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Concepts.Events;

namespace TestNamespace
{
    [EventType]
    public record ItemAdded(string Name);
    
    public class CartState
    {
        public int ItemCount { get; set; }
    }

    public class CartReducer : IReducerFor<CartState>
    {
        public void Added(ItemAdded @event)
        {
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<ReducerMethodAnalyzer>(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task should_report_diagnostic_for_invalid_signature()
    {
        var code = @"
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Concepts.Events;

namespace TestNamespace
{
    [EventType]
    public record ItemAdded(string Name);
    
    public class CartState
    {
        public int ItemCount { get; set; }
    }

    public class CartReducer : IReducerFor<CartState>
    {
        public void Added(ItemAdded @event, string wrongParam)
        {
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<ReducerMethodAnalyzer>(code);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.ReducerMethodSignatureMustMatchAllowed, diagnostics[0].Id);
    }
}
