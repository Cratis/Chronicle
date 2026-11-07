// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphElementAnalyzer.given;

public class a_graph_element_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Collections.Generic;",
            "using Cratis.Chronicle.Keys;",
            "using Cratis.Chronicle.Projections.Graphs;",
            "using Cratis.Arc.Queries.ModelBound;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Keys",
            "{",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class KeyAttribute : Attribute { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Projections.Graphs",
            "{",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]",
            "    public sealed class NodeAttribute : Attribute { }",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]",
            "    public sealed class EdgeAttribute : Attribute { public EdgeAttribute(string type) { } }",
            "}",
            "",
            "namespace Cratis.Arc.Queries.ModelBound",
            "{",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]",
            "    public sealed class ReadModelAttribute : Attribute { }",
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }
}
