// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AutoMapSameNamePropertyCollisionAnalyzer.given;

public class an_auto_map_same_name_property_collision_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using Cratis.Chronicle.Projections;",
            "using Cratis.Chronicle.Projections.ModelBound;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Projections",
            "{",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Class)]",
            "    public sealed class NoAutoMapAttribute : Attribute { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Projections.ModelBound",
            "{",
            "    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]",
            "    public sealed class FromEventAttribute<TEvent> : Attribute { }",
            "",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class SetFromAttribute<TEvent> : Attribute",
            "    {",
            "        public SetFromAttribute(string? eventPropertyName = null) { }",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class JoinAttribute<TEvent> : Attribute",
            "    {",
            "        public JoinAttribute(string? onProperty = null) { }",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class CountAttribute<TEvent> : Attribute { }",
            "",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class SetValueAttribute<TEvent> : Attribute",
            "    {",
            "        public SetValueAttribute(object value) { }",
            "    }",
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }
}
