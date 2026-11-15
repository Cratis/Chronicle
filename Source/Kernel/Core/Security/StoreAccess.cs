// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// The independently granted kinds of event store access.
/// </summary>
[Flags]
public enum StoreAccess
{
    /// <summary>No access.</summary>
    None = 0,

    /// <summary>Read event store content.</summary>
    Read = 1,

    /// <summary>Append events.</summary>
    Append = 2,

    /// <summary>Manage event store resources.</summary>
    Manage = 4
}
