// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_RedundantAutoMapCallAnalyzer.given;

public class a_redundant_auto_map_call_analyzer : Specification
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
            "    public sealed class NoAutoMapAttribute : Attribute { }",
            "    public interface IFromBuilder<TReadModel, TEvent> { }",
            "    public interface IChildrenBuilder<TParent, TChild> : IProjectionBuilder<TChild, IChildrenBuilder<TParent, TChild>>",
            "    {",
            "        IChildrenBuilder<TParent, TChild> IdentifiedBy(Func<TChild, object> key);",
            "    }",
            "    public interface INestedBuilder<TParent, TChild> : IProjectionBuilder<TChild, INestedBuilder<TParent, TChild>> { }",
            "    public interface IProjectionBuilder<TReadModel, TBuilder>",
            "    {",
            "        IProjectionBuilder<TReadModel, TBuilder> AutoMap();",
            "        IProjectionBuilder<TReadModel, TBuilder> NoAutoMap();",
            "        TBuilder Children<TChild>(Func<TReadModel, System.Collections.Generic.IEnumerable<TChild>> target, Action<IChildrenBuilder<TReadModel, TChild>> callback);",
            "        TBuilder Nested<TChild>(Func<TReadModel, TChild> target, Action<INestedBuilder<TReadModel, TChild>> callback);",
            "    }",
            "    public interface IProjectionBuilderFor<TReadModel> : IProjectionBuilder<TReadModel, IProjectionBuilderFor<TReadModel>>",
            "    {",
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
