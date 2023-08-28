// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Integration.for_ImportOperations;

public class when_applying_external_model_with_no_changes : given.no_changes
{
    async Task Because() => await operations.Apply(incoming);

    [Fact] void should_add_causation() => causation_manager.Verify(_ => _.Add(ImportOperations<string, string>.CausationType, IsAny<IDictionary<string, string>>()), Once);
    [Fact] void should_have_adapter_id_in_causation() => causation_properties[ImportOperations<string, string>.CausationAdapterIdProperty].ShouldEqual(adapter_id.ToString());
    [Fact] void should_have_adapter_type_in_causation() => causation_properties[ImportOperations<string, string>.CausationAdapterTypeProperty].ShouldEqual(adapter.Object.GetType().AssemblyQualifiedName);
    [Fact] void should_have_key_in_causation() => causation_properties[ImportOperations<string, string>.CausationKeyProperty].ShouldEqual(key);
    [Fact] void should_not_append_any_events() => event_log.Verify(_ => _.AppendMany(IsAny<EventSourceId>(), IsAny<IEnumerable<EventAndValidFrom>>()), Never);
}
