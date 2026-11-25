// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Storage.InMemory.Registrations.for_RegistrationIntentStorage;

public class when_saving_intents : RegistrationIntentStorageContract
{
    readonly RegistrationIntentStorage _storage = new();
    protected override IRegistrationIntentStorage OpenStorage() => _storage;
}
