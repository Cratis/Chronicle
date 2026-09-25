// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Additive decision-read API on an existing read-model accessor.
/// </summary>
public static class DecisionReadModelsExtensions
{
    /// <summary>
    /// Gets an instance and the exact event-log watermark needed to guard a decision.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="readModels">The read-model accessor.</param>
    /// <param name="key">The event source key.</param>
    /// <returns>The guarded read.</returns>
    /// <exception cref="NotSupportedException">Thrown if the accessor cannot perform decision reads.</exception>
    public static Task<ReadModelInstance<TReadModel>> GetInstanceForDecision<TReadModel>(this IReadModels readModels, ReadModelKey key) =>
        readModels is IDecisionReadModels decisionReadModels
            ? decisionReadModels.GetInstanceForDecision<TReadModel>(key)
            : throw new NotSupportedException("This read-model accessor does not support decision reads.");
}
