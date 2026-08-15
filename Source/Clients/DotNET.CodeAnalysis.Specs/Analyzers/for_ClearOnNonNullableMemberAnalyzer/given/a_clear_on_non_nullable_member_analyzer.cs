// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ClearOnNonNullableMemberAnalyzer.given;

public class a_clear_on_non_nullable_member_analyzer : Specification
{
    protected static string CreateSource(string usage) => CreateSource(usage, nullableEnabled: true);

    /// <summary>
    /// Builds a compilation around the usage, with the Chronicle attributes stubbed so the analyzer needs no
    /// package reference.
    /// </summary>
    /// <param name="usage">The read model declaration under analysis.</param>
    /// <param name="nullableEnabled">Whether the usage is compiled in a nullable-aware context.</param>
    /// <returns>The full source to analyze.</returns>
    /// <remarks>
    /// The nullable context is a parameter because it is the whole question for a reference type: the same
    /// declaration is a promise of non-null in one context and opted out of the analysis in the other, and only the
    /// first can be reported.
    /// </remarks>
    protected static string CreateSource(string usage, bool nullableEnabled)
    {
        return string.Join(Environment.NewLine,
        [
            nullableEnabled ? "#nullable enable" : "#nullable disable",
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
            "    public sealed class SetValueAttribute<TEvent> : Attribute",
            "    {",
            "        public SetValueAttribute(object? value) { }",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]",
            "    public sealed class ClearWithAttribute<TEvent> : Attribute { }",
            "",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class NestedAttribute : Attribute { }",
            "",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]",
            "    public sealed class FromEventAttribute<TEvent> : Attribute",
            "    {",
            "        public FromEventAttribute(string? key = default, string? parentKey = default) { }",
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
