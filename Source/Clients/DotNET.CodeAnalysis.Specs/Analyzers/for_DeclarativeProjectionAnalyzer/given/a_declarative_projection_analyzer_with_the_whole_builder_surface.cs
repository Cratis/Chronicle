// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DeclarativeProjectionAnalyzer.given;

/// <summary>
/// The builder surface as it really is: methods whose type parameter is the event, beside methods whose type
/// parameter is a child read model, a key or a property. Only the first kind can name an event type.
/// </summary>
public class a_declarative_projection_analyzer_with_the_whole_builder_surface : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Collections.Generic;",
            "using System.Linq.Expressions;",
            "using Cratis.Chronicle.Concepts.Events;",
            "",
            "namespace Cratis.Chronicle.Concepts.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeAttribute : Attribute",
            "    {",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Projections",
            "{",
            "    public interface IChildrenBuilder<TParentReadModel, TChildReadModel>",
            "    {",
            "        IChildrenBuilder<TParentReadModel, TChildReadModel> IdentifiedBy<TProperty>(Expression<Func<TChildReadModel, TProperty>> propertyExpression);",
            "    }",
            "",
            "    public interface IJoinBuilder<TReadModel, TEvent>",
            "    {",
            "        IJoinBuilder<TReadModel, TEvent> On<TProperty>(Expression<Func<TReadModel, TProperty>> keyAccessor);",
            "    }",
            "",
            "    public interface IProjectionBuilderFor<TReadModel>",
            "    {",
            "        IProjectionBuilderFor<TReadModel> From<TEvent>();",
            "        IProjectionBuilderFor<TReadModel> Join<TEvent>(Action<IJoinBuilder<TReadModel, TEvent>> builderCallback);",
            "        IProjectionBuilderFor<TReadModel> Children<TChildModel>(Expression<Func<TReadModel, IEnumerable<TChildModel>>> targetProperty, Action<IChildrenBuilder<TReadModel, TChildModel>> builderCallback);",
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
