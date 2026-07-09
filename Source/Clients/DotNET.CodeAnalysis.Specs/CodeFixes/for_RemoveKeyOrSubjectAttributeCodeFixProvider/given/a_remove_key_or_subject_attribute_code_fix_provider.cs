// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_RemoveKeyOrSubjectAttributeCodeFixProvider.given;

public class a_remove_key_or_subject_attribute_code_fix_provider : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using Cratis.Chronicle;",
            "using Cratis.Chronicle.Keys;",
            "using Cratis.Chronicle.Events;",
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
            "namespace Cratis.Chronicle",
            "{",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class SubjectAttribute : Attribute { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Events",
            "{",
            "    public record EventSourceId<T>(T Value);",
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }
}
