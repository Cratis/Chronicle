// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentJoinOverridesLocalWriteAnalyzer.given;

public class a_fluent_join_overrides_local_write_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Linq.Expressions;",
            "using Cratis.Chronicle.Projections;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Projections",
            "{",
            "    public interface ISetBuilder<TReadModel, TEvent, TProperty, TParentBuilder>",
            "    {",
            "        TParentBuilder To<TEventProperty>(Expression<Func<TEvent, TEventProperty>> accessor);",
            "        TParentBuilder ToValue(TProperty value);",
            "    }",
            "",
            "    public interface IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder>",
            "    {",
            "        ISetBuilder<TReadModel, TEvent, TProperty, TBuilder> Set<TProperty>(Expression<Func<TReadModel, TProperty>> accessor);",
            "        TBuilder Increment<TProperty>(Expression<Func<TReadModel, TProperty>> accessor);",
            "    }",
            "",
            "    public interface IFromBuilder<TReadModel, TEvent> : IReadModelPropertiesBuilder<TReadModel, TEvent, IFromBuilder<TReadModel, TEvent>>",
            "    {",
            "    }",
            "",
            "    public interface IJoinBuilder<TReadModel, TEvent> : IReadModelPropertiesBuilder<TReadModel, TEvent, IJoinBuilder<TReadModel, TEvent>>",
            "    {",
            "        IJoinBuilder<TReadModel, TEvent> On<TProperty>(Expression<Func<TReadModel, TProperty>> keyAccessor);",
            "    }",
            "",
            "    public interface IProjectionBuilder<TReadModel, TBuilder>",
            "    {",
            "        TBuilder From<TEvent>(Action<IFromBuilder<TReadModel, TEvent>>? builderCallback = default);",
            "        TBuilder Join<TEvent>(Action<IJoinBuilder<TReadModel, TEvent>>? builderCallback = default);",
            "    }",
            "",
            "    public interface IProjectionBuilderFor<TReadModel> : IProjectionBuilder<TReadModel, IProjectionBuilderFor<TReadModel>>",
            "    {",
            "    }",
            "",
            "    public interface IProjectionFor<TReadModel>",
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
