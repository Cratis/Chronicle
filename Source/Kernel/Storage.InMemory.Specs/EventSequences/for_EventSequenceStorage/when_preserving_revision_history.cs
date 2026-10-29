// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_preserving_revision_history : EventRevisionContract
{
    IEventSequenceStorage? _storage;

    protected override Task<IEventSequenceStorage> OpenStorage() =>
        Task.FromResult(_storage ??= new EventSequenceStorage("test-store", "test-namespace", EventSequenceId.Log, Identities));
}
