// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaims;

namespace Cratis.Chronicle.Storage.InMemory.Generations.Bindings.for_InMemoryObserverIdentityClaims;

public class when_releasing_another_owners_claim : Contract.when_releasing_another_owners_claim<InMemoryMaterializationBindingStorageHarness>;
