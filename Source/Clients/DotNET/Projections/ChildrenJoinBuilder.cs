// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
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
    /// <summary>
    /// Builds a <see cref="JoinDefinition"/> from the builder.
    /// </summary>
    /// <returns>A new instance of <see cref="JoinDefinition"/>.</returns>
    /// <exception cref="MissingIdentifiedByPropertyExpressionWhenJoiningWithEvent">The child identity was not configured.</exception>
    internal JoinDefinition Build()
    {
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
