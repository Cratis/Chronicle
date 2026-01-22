// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the source of a read model.
/// </summary>
public enum ReadModelSource
{
    /// <summary>
    /// The source is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The read model is from the core system.
    /// </summary>
    Code = 1,

    /// <summary>
    /// The read model is from a user.
    /// </summary>
    User = 2
}
