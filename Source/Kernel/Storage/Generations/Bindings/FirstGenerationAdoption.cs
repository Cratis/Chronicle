// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the first generation adoption.
/// </summary>
public enum FirstGenerationAdoption
{
    /// <summary>
    /// Adopted.
    /// </summary>
    Adopted = 0,

    /// <summary>
    /// AlreadyAdopted.
    /// </summary>
    AlreadyAdopted = 1,

    /// <summary>
    /// Conflicting.
    /// </summary>
    Conflicting = 2
}
