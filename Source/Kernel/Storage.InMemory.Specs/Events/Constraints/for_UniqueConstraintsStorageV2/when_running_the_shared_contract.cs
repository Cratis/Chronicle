// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_running_the_shared_contract : UniqueConstraintsStorageV2Contract
{
    protected override Task<IUniqueConstraintsStorageV2> CreateStorage() => Task.FromResult<IUniqueConstraintsStorageV2>(new UniqueConstraintsStorageV2());
}
