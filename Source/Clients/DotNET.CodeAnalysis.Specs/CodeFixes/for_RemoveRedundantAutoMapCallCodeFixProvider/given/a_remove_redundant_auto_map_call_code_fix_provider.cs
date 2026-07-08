// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_RemoveRedundantAutoMapCallCodeFixProvider.given;

public class a_remove_redundant_auto_map_call_code_fix_provider : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "",
            "namespace Cratis.Chronicle.Projections",
            "{",
            "    public interface IProjection { }",
            "    public interface IFromBuilder<TReadModel, TEvent> { }",
            "    public interface IProjectionBuilderFor<TReadModel>",
            "    {",
            "        IProjectionBuilderFor<TReadModel> AutoMap();",
            "        IProjectionBuilderFor<TReadModel> NoAutoMap();",
            "        IProjectionBuilderFor<TReadModel> From<TEvent>(Action<IFromBuilder<TReadModel, TEvent>> callback = default);",
            "    }",
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
