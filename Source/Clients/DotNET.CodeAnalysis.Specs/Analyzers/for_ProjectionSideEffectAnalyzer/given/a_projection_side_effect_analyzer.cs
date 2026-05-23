// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ProjectionSideEffectAnalyzer.given;

public class a_projection_side_effect_analyzer : Specification
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
            "namespace Cratis.Chronicle.Projections",
            "{",
            "    public interface IProjection { }",
            "    public interface IProjectionBuilderFor<TReadModel> { }",
            "    public interface IProjectionFor<TReadModel> : IProjection",
            "    {",
            "        void Define(IProjectionBuilderFor<TReadModel> builder);",
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
