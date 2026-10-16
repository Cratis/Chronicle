// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_LifecycleFanOutStorage;

public class when_storing_an_intent : LifecycleFanOutContract
{
    readonly LifecycleFanOutStorage _storage = new();
    protected override ILifecycleFanOutStorage OpenStorage() => _storage;
}
