// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConstraintImperativeCodeAnalyzer.given;

public class a_constraint_imperative_code_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "",
            "namespace Cratis.Chronicle.Events.Constraints",
            "{",
            "    public interface IConstraintBuilder",
            "    {",
            "        IConstraintBuilder PerEventSourceType();",
            "    }",
            "    public interface IConstraint",
            "    {",
            "        void Define(IConstraintBuilder builder);",
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
