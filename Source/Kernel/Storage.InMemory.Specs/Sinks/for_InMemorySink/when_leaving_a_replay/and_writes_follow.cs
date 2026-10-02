// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_leaving_a_replay;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_leaving_a_replay;

public class and_writes_follow : Contract.and_writes_follow<InMemorySinkHarness>;
