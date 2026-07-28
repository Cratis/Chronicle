// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.given;

public class a_fluent_cross_subject_pii_join_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Linq.Expressions;",
            "using Cratis.Chronicle.Compliance.GDPR;",
            "using Cratis.Chronicle.Keys;",
            "using Cratis.Chronicle.Projections;",
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
            "namespace Cratis.Chronicle.Keys",
            "{",
            "    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]",
            "    public sealed class KeyAttribute : Attribute { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Projections",
            "{",
            "    public interface ISetBuilder<TReadModel, TEvent, TProperty, TParentBuilder>",
            "    {",
            "        TParentBuilder To<TEventProperty>(Expression<Func<TEvent, TEventProperty>> accessor);",
            "    }",
            "",
            "    public interface IJoinBuilder<TReadModel, TEvent>",
            "    {",
            "        IJoinBuilder<TReadModel, TEvent> On<TProperty>(Expression<Func<TReadModel, TProperty>> keyAccessor);",
            "        ISetBuilder<TReadModel, TEvent, TProperty, IJoinBuilder<TReadModel, TEvent>> Set<TProperty>(Expression<Func<TReadModel, TProperty>> accessor);",
            "    }",
            "",
            "    public interface IProjectionBuilder<TReadModel, TBuilder>",
            "    {",
            "        TBuilder From<TEvent>();",
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
