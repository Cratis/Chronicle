// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ProjectionExpressionLambdaAnalyzer.given;

public class a_projection_expression_lambda_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Linq.Expressions;",
            "",
            "namespace Cratis.Chronicle.Projections",
            "{",
            "    public interface IProjection { }",
            "    public interface ISetBuilder<TReadModel, TEvent, TProperty>",
            "    {",
            "        void To(Expression<Func<TEvent, TProperty>> accessor);",
            "    }",
            "    public interface IFromBuilder<TReadModel, TEvent>",
            "    {",
            "        ISetBuilder<TReadModel, TEvent, TProperty> Set<TProperty>(Expression<Func<TReadModel, TProperty>> accessor);",
            "    }",
            "    public interface IProjectionBuilderFor<TReadModel>",
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
