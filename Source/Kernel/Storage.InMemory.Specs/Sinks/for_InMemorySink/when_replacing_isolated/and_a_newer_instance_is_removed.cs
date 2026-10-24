// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_replacing_isolated;

public class and_a_newer_instance_is_removed() : Contract.a_conditional_replacement<InMemorySinkHarness>(30, true, IsolatedReplaceResult.Superseded, 10);
