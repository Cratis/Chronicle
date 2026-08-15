// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ClearOnNonNullableMemberAnalyzer.given;

public class a_fluent_clear_on_non_nullable_member_analyzer : Specification
{
    /// <summary>
    /// Builds a compilation around a fluent projection, stubbing the whole builder surface the analyzer resolves.
    /// </summary>
    /// <param name="usage">The projection declaration under analysis.</param>
    /// <returns>The full source to analyze.</returns>
    /// <remarks>
    /// Every interface <c>FluentProjectionSymbols.TryCreate</c> looks up has to be present: it resolves the set as
    /// a whole and returns nothing if any one is missing, which would switch the analyzer off and leave every spec
    /// over this harness silently green. The positive specs are what prove it actually activated.
    /// </remarks>
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "#nullable enable",
            "using System;",
            "using System.Collections.Generic;",
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
            "    public interface ISetBuilder<TReadModel, TEvent, TParentBuilder>",
            "    {",
            "        TParentBuilder ToEventSourceId();",
            "    }",
            "",
            "    public interface ISetBuilder<TReadModel, TEvent, TProperty, TParentBuilder> : ISetBuilder<TReadModel, TEvent, TParentBuilder>",
            "    {",
            "        TParentBuilder To<TEventProperty>(Expression<Func<TEvent, TEventProperty>> accessor);",
            "        TParentBuilder ToValue(TProperty value);",
            "    }",
            "",
            "    public interface IAddBuilder<TReadModel, TEvent, TProperty, TParentBuilder>",
            "    {",
            "        TParentBuilder With<TEventProperty>(Expression<Func<TEvent, TEventProperty>> accessor);",
            "    }",
            "",
            "    public interface ISubtractBuilder<TReadModel, TEvent, TProperty, TParentBuilder>",
            "    {",
            "        TParentBuilder With<TEventProperty>(Expression<Func<TEvent, TEventProperty>> accessor);",
            "    }",
            "",
            "    public interface IAddChildBuilder<TChildModel, TEvent>",
            "    {",
            "    }",
            "",
            "    public interface IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder>",
            "    {",
            "        ISetBuilder<TReadModel, TEvent, TProperty, TBuilder> Set<TProperty>(Expression<Func<TReadModel, TProperty>> accessor);",
            "        TBuilder Clear<TProperty>(Expression<Func<TReadModel, TProperty>> accessor);",
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
            "    public interface IChildrenBuilder<TParentReadModel, TChildReadModel> : IProjectionBuilder<TChildReadModel, IChildrenBuilder<TParentReadModel, TChildReadModel>>",
            "    {",
            "        IChildrenBuilder<TParentReadModel, TChildReadModel> IdentifiedBy<TProperty>(Expression<Func<TChildReadModel, TProperty>> accessor);",
            "    }",
            "",
            "    public interface IProjectionBuilder<TReadModel, TBuilder>",
            "    {",
            "        TBuilder From<TEvent>(Action<IFromBuilder<TReadModel, TEvent>>? builderCallback = default);",
            "        TBuilder Join<TEvent>(Action<IJoinBuilder<TReadModel, TEvent>>? builderCallback = default);",
            "        TBuilder Children<TChildModel>(Expression<Func<TReadModel, IEnumerable<TChildModel>>> targetProperty, Action<IChildrenBuilder<TReadModel, TChildModel>> builderCallback);",
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
