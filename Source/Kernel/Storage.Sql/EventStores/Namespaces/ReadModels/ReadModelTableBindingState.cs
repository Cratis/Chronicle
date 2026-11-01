// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// The durable provision state; removal never releases ownership.
/// </summary>
internal enum ReadModelTableBindingState
{
    /// <summary>Committed ownership before provisioning finishes.</summary>
    Reserved = 0,

    /// <summary>The owner successfully provisioned or adopted the table.</summary>
    Ready = 1,

    /// <summary>Intentional removal; the physical name remains reserved.</summary>
    Removed = 2
}
