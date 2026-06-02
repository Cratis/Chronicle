// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConstraintSideEffectAnalyzer.given;

public class a_constraint_side_effect_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "",
            "namespace Cratis.Chronicle.EventSequences",
            "{",
            "    public interface IEventSequence { }",
            "    public interface IEventLog : IEventSequence { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Commands",
            "{",
            "    public interface ICommandPipeline { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Events.Constraints",
            "{",
            "    public interface IConstraintBuilder { }",
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
