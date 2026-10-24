// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the generation transition result.
/// </summary>
public enum GenerationTransitionResult
{
    /// <summary>
    /// Transitioned.
    /// </summary>
    Transitioned = 0,

    /// <summary>
    /// AlreadyInState.
    /// </summary>
    AlreadyInState = 1,

    /// <summary>
    /// Refused.
    /// </summary>
    Refused = 2,

    /// <summary>
    /// UnknownGeneration.
    /// </summary>
    UnknownGeneration = 3
}
