// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation.Registrations;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Registrations.for_ObserverRegistrationsStorage;

public class when_persisting_registrations : ObserverRegistrationsStorageContract
{
    readonly ObserverRegistrationsStorage _storage = new(SerializerOptions);

    protected override IObserverRegistrationsStorage Open() => _storage;

    protected override Task<IObserverRegistrationsStorage> OpenOtherStore() => Task.FromResult<IObserverRegistrationsStorage>(new ObserverRegistrationsStorage(SerializerOptions));
}
