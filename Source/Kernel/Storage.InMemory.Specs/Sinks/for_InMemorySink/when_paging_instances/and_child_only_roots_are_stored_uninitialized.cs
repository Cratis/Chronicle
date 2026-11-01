// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_paging_instances;

public class and_child_only_roots_are_stored_uninitialized : Contract.and_child_only_roots_are_stored_uninitialized<InMemorySinkHarness>;
