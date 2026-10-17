// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_blocked_definition_write : an_observer_quarantined_during_a_probe
{
    protected readonly blockable_definition_write _definitionWrite = new();

    public an_observer_with_blocked_definition_write() =>
        _silo.Options.StorageFactory = type => type == typeof(ObserverDefinition) ? _definitionWrite : null!;

    void Establish() => _definitionWrite.Block = true;
}
