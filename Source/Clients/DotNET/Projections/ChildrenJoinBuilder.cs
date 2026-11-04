// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IChildrenJoinBuilder{TReadModel, TEvent}"/>.
/// </summary>
/// <param name="childrenBuilder">The children builder providing the child identity.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <typeparam name="TParentReadModel">Parent read model type.</typeparam>
/// <typeparam name="TChildReadModel">Child read model type.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public class ChildrenJoinBuilder<TParentReadModel, TChildReadModel, TEvent>(IChildrenBuilder<TParentReadModel, TChildReadModel> childrenBuilder, INamingPolicy namingPolicy)
    : ReadModelPropertiesBuilder<TChildReadModel, TEvent, IChildrenJoinBuilder<TChildReadModel, TEvent>, IChildrenBuilder<TParentReadModel, TChildReadModel>>(childrenBuilder, namingPolicy),
      IChildrenJoinBuilder<TChildReadModel, TEvent>
{
    readonly INamingPolicy _namingPolicy = namingPolicy;
    PropertyPath? _on;

    /// <inheritdoc/>
    [Obsolete("Child joins always join on the identity set with IdentifiedBy() on the children builder, and specifying On() throws OnPropertyShouldNotBeSpecifiedForChildJoin when the projection is built. Remove the call - On() on child joins will be removed in the next major version.")]
    public IChildrenJoinBuilder<TChildReadModel, TEvent> On<TProperty>(Expression<Func<TChildReadModel, TProperty>> keyAccessor)
    {
        _on = ProjectionPropertyPaths.FromExpression(keyAccessor, _namingPolicy, ProjectionPathRole.Target, $"the join 'on' property on read model '{typeof(TChildReadModel).FullName}'");
        return this;
    }

    /// <summary>
    /// Builds a <see cref="JoinDefinition"/> from the builder.
    /// </summary>
    /// <returns>A new instance of <see cref="JoinDefinition"/>.</returns>
    /// <exception cref="OnPropertyShouldNotBeSpecifiedForChildJoin">An explicit join property was specified.</exception>
    /// <exception cref="MissingIdentifiedByPropertyExpressionWhenJoiningWithEvent">The child identity was not configured.</exception>
    internal JoinDefinition Build()
    {
        if (_on is not null)
        {
            throw new OnPropertyShouldNotBeSpecifiedForChildJoin(typeof(TChildReadModel), typeof(TEvent));
        }

        if (!childrenBuilder.HasIdentifiedBy)
        {
            throw new MissingIdentifiedByPropertyExpressionWhenJoiningWithEvent(typeof(TChildReadModel), typeof(TEvent));
        }

        return new()
        {
            On = childrenBuilder.GetIdentifiedBy(),
            Properties = _propertyExpressions.ToDictionary(_ => (string)_.Key, _ => _.Value.Build()),
            Key = _keyExpression
        };
    }
}
