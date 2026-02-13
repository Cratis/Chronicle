// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the state of Data Protection keys.
/// </summary>
public class DataProtectionKeysState
{
    /// <summary>
    /// Gets the keys.
    /// </summary>
    public IList<DataProtectionKey> Keys { get; init; } = [];

    /// <summary>
    /// Gets the new keys to be persisted.
    /// </summary>
    public IList<DataProtectionKey> NewKeys { get; init; } = [];
}
