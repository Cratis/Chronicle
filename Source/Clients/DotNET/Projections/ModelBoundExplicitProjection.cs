// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an explicitly registered read model whose projection is declared by model-bound attributes.
/// </summary>
/// <param name="readModelType">Type of read model carrying the model-bound attributes.</param>
internal sealed class ModelBoundExplicitProjection(Type readModelType) : IExplicitProjection
{
    /// <inheritdoc/>
    public Type ReadModelType => readModelType;

    /// <inheritdoc/>
    /// <remarks>
    /// The same identifier discovery gives a model-bound projection, so registering a read model explicitly that
    /// discovery also found is the same projection rather than a competing one.
    /// </remarks>
    public ProjectionId Id => new(readModelType.FullName!);

    /// <inheritdoc/>
    public bool IsModelBound => true;

    /// <inheritdoc/>
    /// <exception cref="ReadModelIsNotModelBound">Thrown when the read model carries no model-bound projection attributes.</exception>
    /// <exception cref="VariantReadModelsCannotBeRegisteredExplicitly">Thrown when the read model is a variant or a global handler of one.</exception>
    public ProjectionDefinition Build(ExplicitProjectionContext context)
    {
        if (!readModelType.HasModelBoundProjectionAttributes())
        {
            throw new ReadModelIsNotModelBound(readModelType);
        }

        if (readModelType.TryGetVariantIdentity(out _) || readModelType.TryGetGlobalForIdentity(out _))
        {
            throw new VariantReadModelsCannotBeRegisteredExplicitly(readModelType);
        }

        return new ModelBoundProjectionBuilder(context.NamingPolicy, context.EventTypes, context.EventStoreName).Build(readModelType);
    }
}
