// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_PIIAndEncryptedCombinedAnalyzer.given;

public class a_pii_and_encrypted_combined_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using Cratis.Chronicle.Compliance.GDPR;",
            "using Cratis.Chronicle.ProtectedValues;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Compliance.GDPR",
            "{",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class PIIAttribute : Attribute { }",
            "}",
            "",
            "namespace Cratis.Chronicle.ProtectedValues",
            "{",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class EncryptedAttribute : Attribute { }",
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }
}
