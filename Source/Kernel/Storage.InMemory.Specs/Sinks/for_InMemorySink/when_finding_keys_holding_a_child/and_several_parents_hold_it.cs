// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_finding_keys_holding_a_child;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_finding_keys_holding_a_child;

public class and_several_parents_hold_it : Contract.and_several_parents_hold_it<InMemorySinkHarness>;
