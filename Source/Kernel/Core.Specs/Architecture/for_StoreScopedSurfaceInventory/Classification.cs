// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Architecture.for_StoreScopedSurfaceInventory;

public record Classification(Entitlement Entitlement, string? Reason = null, string? StoreArgumentException = null)
{
    public bool IsStoreScoped => (Entitlement & (Entitlement.StoreRead | Entitlement.StoreAppend | Entitlement.StoreManage)) != Entitlement.None;
}
