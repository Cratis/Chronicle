// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

public interface IMaterializationBindingStorageHarness : IDisposable
{
    IObserverIdentityClaims Claims { get; }
    IMaterializationBindingStorage Create(TimeProvider? timeProvider = null);
    Task SaveObserverDefinition(EventStoreName store, ObserverId observerId);
}
