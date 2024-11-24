// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_ImportOperations;

public class when_applying_external_model_with_no_changes : given.no_changes
{
    async Task Because() => await _operations.Apply(_incoming);

    [Fact] void should_add_causation() => causation_manager.Received(1).Add(ImportOperations<string, string>.CausationType, Arg.Any<IDictionary<string, string>>());
    [Fact] void should_have_adapter_id_in_causation() => _causationProperties[ImportOperations<string, string>.CausationAdapterIdProperty].ShouldEqual(_adapterId.ToString());
    [Fact] void should_have_adapter_type_in_causation() => _causationProperties[ImportOperations<string, string>.CausationAdapterTypeProperty].ShouldEqual(_adapter.GetType().AssemblyQualifiedName);
    [Fact] void should_have_key_in_causation() => _causationProperties[ImportOperations<string, string>.CausationKeyProperty].ShouldEqual(key);
}
