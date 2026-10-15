// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Seeding;

/// <summary>
/// Describes how an entry joined the stored global seed set.
/// </summary>
public enum SeedOrigin
{
    /// <summary>
    /// An entry stored before ownership was recorded.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// An entry in a program's authoritative declaration.
    /// </summary>
    Declared = 1,

    /// <summary>
    /// An entry appended without replacing a program's declaration.
    /// </summary>
    Added = 2
}
