// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.InMemory.Occurrences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistrar.given;

public class a_registrar : Specification
{
    protected TriggerRegistrar _registrar;
    protected IStorage _storage;
    protected ITriggerDefinitionsStorage _definitions;
    protected RegisterTriggers _command;
    protected TriggerRegistrationResult _result;
    protected TriggerDefinition? _stored;

    void Establish()
    {
        _registrar = new();
        _definitions = new TriggerDefinitionsStorage();
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStore(new EventStoreName("occurrences")).TriggerDefinitions.Returns(_definitions);
        _command = new(new EventStoreName("occurrences"), [new("payment-import", "Payment importer", "{\"type\":\"object\",\"properties\":{\"count\":{\"type\":\"integer\"}}}")]);
    }
}
