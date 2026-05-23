// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConstraintExpressionLambdaAnalyzer.given;

public class a_constraint_expression_lambda_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Linq.Expressions;",
            "",
            "namespace Cratis.Chronicle.Events.Constraints",
            "{",
            "    public interface IUniqueConstraintBuilder",
            "    {",
            "        IUniqueConstraintBuilder On<TEventType>(params Expression<Func<TEventType, object>>[] properties);",
            "    }",
            "    public interface IConstraintBuilder",
            "    {",
            "        IConstraintBuilder Unique(Action<IUniqueConstraintBuilder> callback);",
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
