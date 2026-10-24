// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.InMemory.Generations.Bindings;

public sealed class InMemoryMaterializationBindingStorageHarness : IMaterializationBindingStorageHarness
{
    public IMaterializationBindingStorage Create(TimeProvider? timeProvider = null) => new InMemoryMaterializationBindingStorage(timeProvider);
    public void Dispose() { }
}
