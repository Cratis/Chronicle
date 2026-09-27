// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system that knows which read models are maintained by passive projections.
/// </summary>
internal interface IKnowPassiveProjections
{
    /// <summary>
    /// Check whether the projection maintaining a read model is passive - computed on demand rather than materialized.
    /// </summary>
    /// <param name="readModelType">The read model type to check.</param>
    /// <returns>True if the read model is maintained by a passive projection, false otherwise.</returns>
    /// <remarks>
    /// A projection is made passive either by <see cref="ReadModels.PassiveAttribute"/> on its read model or by
    /// <see cref="IProjectionBuilderFor{TReadModel}.Passive"/> in its definition. Only the definition knows about the latter,
    /// so this answers from the definition rather than from the read model type.
    /// </remarks>
    bool IsPassive(Type readModelType);
}
