// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

namespace Cratis.Chronicle.Storage.InMemory.Generations.Bindings.for_InMemoryMaterializationBindingStorage;

public class when_reusing_an_operation_with_changed_inputs : Contract.when_reusing_an_operation_with_changed_inputs<InMemoryMaterializationBindingStorageHarness>;
