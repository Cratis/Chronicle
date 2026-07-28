// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_CrossSubjectPiiJoinAnalyzer.given;

public class a_cross_subject_pii_join_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using Cratis.Chronicle;",
            "using Cratis.Chronicle.Compliance.GDPR;",
            "using Cratis.Chronicle.Events;",
            "using Cratis.Chronicle.Keys;",
            "using Cratis.Chronicle.Projections.ModelBound;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Chronicle",
            "{",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class SubjectAttribute : Attribute { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Compliance.GDPR",
            "{",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class PIIAttribute : Attribute { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Events",
            "{",
            "    public record EventSourceId<T>(T Value);",
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
            "    public sealed class JoinAttribute<TEvent> : Attribute",
            "    {",
            "        public JoinAttribute(string? on = null, string? eventPropertyName = null) { }",
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
