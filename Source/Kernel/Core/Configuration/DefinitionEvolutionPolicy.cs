// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Specifies how Chronicle applies the evolution required by a changed projection or reducer definition.
/// </summary>
public enum DefinitionEvolutionPolicy
{
    /// <summary>
    /// Applies no-action, partial replay, and full replay plans automatically.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Applies no-action and partial replay plans automatically, but creates a recommendation for a full replay.
    /// </summary>
    PartialOnly = 1,

    /// <summary>
    /// Does not replay automatically and creates a recommendation whenever existing read models may be affected.
    /// </summary>
    Manual = 2
}
