// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_upserting_guarded;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_upserting_guarded;

public class and_the_watermark_is_newer() : Contract.a_guarded_upsert<InMemorySinkHarness>(30, 10);
