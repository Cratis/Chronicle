// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ChildrenCollectionPropertyAutoMapAnalyzer.given;

public class a_children_collection_property_auto_map_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Collections.Generic;",
            "using Cratis.Chronicle.Keys;",
            "using Cratis.Chronicle.Projections.ModelBound;",
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
            "namespace Cratis.Chronicle.Projections.ModelBound",
            "{",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]",
            "    public sealed class ChildrenFromAttribute<TEvent> : Attribute",
            "    {",
            "        public ChildrenFromAttribute(string? key = null, string? identifiedBy = null, string? parentKey = null) { }",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]",
            "    public sealed class SetFromAttribute<TEvent> : Attribute",
            "    {",
            "        public SetFromAttribute(string? eventPropertyName = null) { }",
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
