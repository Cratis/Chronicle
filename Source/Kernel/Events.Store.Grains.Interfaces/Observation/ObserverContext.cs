// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Cratis. All rights reserved.

using Cratis.Execution;

namespace Cratis.Events.Store.Grains.Observation
{
    public record ObserverContext(Guid id, TenantId tenantId);
}
