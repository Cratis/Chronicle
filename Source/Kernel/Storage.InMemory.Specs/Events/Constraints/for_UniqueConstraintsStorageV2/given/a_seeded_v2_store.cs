// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2.given;

public class a_seeded_v2_store : a_v2_store
{
    async Task Establish() => await Seed();
}
