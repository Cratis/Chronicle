// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents where the System event store's data is kept.
/// </summary>
public enum SystemEventStoreLocation
{
    /// <summary>
    /// Where the System event store's data was kept before its location became configurable: named after the event store.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// Where the current configuration says the System event store's data is kept.
    /// </summary>
    Configured = 1
}
