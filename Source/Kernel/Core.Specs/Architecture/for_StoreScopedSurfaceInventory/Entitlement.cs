// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Architecture.for_StoreScopedSurfaceInventory;

/// <summary>
/// Describes the surface, not current authorization enforcement or a grant hierarchy.
/// </summary>
[Flags]
public enum Entitlement
{
    None = 0,
    StoreRead = 1,
    StoreAppend = 2,
    StoreManage = 4,
    SystemOnlyOrAdmin = 8,
    ConnectionScoped = 16,
    Unscoped = 32
}
