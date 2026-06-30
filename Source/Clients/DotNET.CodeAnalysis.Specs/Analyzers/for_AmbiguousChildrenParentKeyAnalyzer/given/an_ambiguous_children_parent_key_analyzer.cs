// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AmbiguousChildrenParentKeyAnalyzer.given;

public class an_ambiguous_children_parent_key_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Collections.Generic;",
            "using Cratis.Chronicle.Projections.ModelBound;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Projections.ModelBound",
            "{",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]",
            "    public sealed class ChildrenFromAttribute<TEvent> : Attribute",
            "    {",
            "        public ChildrenFromAttribute(string? key = null, string? identifiedBy = null, string? parentKey = null) { }",
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
