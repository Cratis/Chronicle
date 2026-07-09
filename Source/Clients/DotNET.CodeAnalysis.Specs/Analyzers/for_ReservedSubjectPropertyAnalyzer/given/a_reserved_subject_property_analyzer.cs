// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReservedSubjectPropertyAnalyzer.given;

public class a_reserved_subject_property_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using Cratis.Arc.Queries.ModelBound;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Arc.Queries.ModelBound",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
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
