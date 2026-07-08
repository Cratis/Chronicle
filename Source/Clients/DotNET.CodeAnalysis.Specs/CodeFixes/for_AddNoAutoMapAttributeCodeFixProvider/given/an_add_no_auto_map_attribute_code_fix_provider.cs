// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_AddNoAutoMapAttributeCodeFixProvider.given;

public class an_add_no_auto_map_attribute_code_fix_provider : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
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
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }
}
