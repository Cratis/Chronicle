// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_ending_a_replay;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_ending_a_replay;

public class and_the_replay_produced_writes : Contract.and_the_replay_produced_writes<InMemorySinkHarness>;
