// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

namespace Cratis.Chronicle.Storage.InMemory.Generations.Bindings.for_InMemoryMaterializationBindingStorage;

public class when_restoring_a_generation_that_is_not_retired : Contract.when_restoring_a_generation_that_is_not_retired<InMemoryMaterializationBindingStorageHarness>;
