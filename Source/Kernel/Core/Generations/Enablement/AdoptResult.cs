// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Describes an immutable generation-one adoption attempt.
/// </summary>
public enum AdoptResult
{
    /// <summary>
    /// The generation was adopted.
    /// </summary>
    Adopted = 0,

    /// <summary>
    /// The identical generation was already adopted.
    /// </summary>
    AlreadyAdopted = 1,

    /// <summary>
    /// An incompatible reservation already exists.
    /// </summary>
    Conflicting = 2
}
