// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.given;

public class a_duplicate_set_from_context_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Collections.Generic;",
            "using Cratis.Chronicle.Concepts.Events;",
            "using Cratis.Chronicle.Projections.ModelBound;",
            "",
            "namespace Cratis.Chronicle.Concepts.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeAttribute : Attribute",
            "    {",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Projections.ModelBound",
            "{",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]",
            "    public sealed class SetFromContextAttribute<TEvent> : Attribute",
            "    {",
            "        public SetFromContextAttribute(string contextPropertyName = null) { }",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]",
            "    public sealed class ChildrenFromAttribute<TEvent> : Attribute",
            "    {",
            "        public ChildrenFromAttribute(string key = null) { }",
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
