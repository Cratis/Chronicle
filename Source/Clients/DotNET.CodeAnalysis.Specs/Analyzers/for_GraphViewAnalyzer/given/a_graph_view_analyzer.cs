// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphViewAnalyzer.given;

public class a_graph_view_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Collections.Generic;",
            "using Cratis.Chronicle.Keys;",
            "using Cratis.Chronicle.Graphs;",
            "using Cratis.Chronicle.Projections.Graphs;",
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
            "    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]",
            "    public sealed class SourceAttribute<TNode> : Attribute { }",
            "    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]",
            "    public sealed class TargetAttribute<TNode> : Attribute { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Graphs",
            "{",
            "    public enum GraphViewDirection { Outgoing = 0, Incoming = 1 }",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]",
            "    public sealed class ViewOfAttribute<TNode> : Attribute { }",
            "    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]",
            "    public sealed class FollowsAttribute<TEdge> : Attribute { public FollowsAttribute() { } public FollowsAttribute(GraphViewDirection direction) { } }",
            "    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]",
            "    public sealed class FromEdgeAttribute : Attribute { }",
            "}",
            "",
            "namespace Sample",
            "{",
            "    [Node] public record Person([Key] Guid PersonId, string Name);",
            "    [Node] public record Group([Key] Guid GroupId, string Name);",
            "    [Edge(\"MEMBER_OF\")] public record MemberOf([Source<Person>] Guid PersonId, [Target<Group>] Guid GroupId, DateTimeOffset Since);",
            usage,
            "}"
        ]);
    }
}
