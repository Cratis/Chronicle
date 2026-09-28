// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an explicitly registered projection defined through the same builder as <see cref="IProjectionFor{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of read model the projection maintains.</typeparam>
/// <param name="define">The callback that defines the projection.</param>
/// <param name="id">The identifier of the projection.</param>
internal sealed class DeclarativeExplicitProjection<TReadModel>(Action<IProjectionBuilderFor<TReadModel>> define, ProjectionId id) : IExplicitProjection
{
    /// <inheritdoc/>
    public Type ReadModelType => typeof(TReadModel);

    /// <inheritdoc/>
    public ProjectionId Id => id;

    /// <inheritdoc/>
    public bool IsModelBound => false;

    /// <inheritdoc/>
    /// <exception cref="VariantReadModelsCannotBeRegisteredExplicitly">Thrown when the definition declares the read model a variant.</exception>
    public ProjectionDefinition Build(ExplicitProjectionContext context)
    {
        var builder = new ProjectionBuilderFor<TReadModel>(
            id,
            typeof(TReadModel),
            context.NamingPolicy,
            context.EventTypes,
            context.JsonSerializerOptions);
        define(builder);

        // A variant is cross-wired with its siblings, which are only ever found together by discovery. An explicit
        // registration names one read model and nothing about the rest of its group, so it cannot take part.
        if (builder.VariantDeclaration is not null)
        {
            throw new VariantReadModelsCannotBeRegisteredExplicitly(typeof(TReadModel));
        }

        return builder.Build();
    }
}
