// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_application_metadata;

public class and_event_store_names_differ : given.application_metadata_sharing
{
    void Because()
    {
        _other = new(null, _artifacts, null, new ScenarioTime(), _name, _namespace);
        _third = new(null, _artifacts, null, new ScenarioTime(), _name, _namespace);
    }

    [Fact] void should_not_share_with_a_store_of_another_name() => _store.Metadata.ShouldNotBeSame(_other.Metadata);
    [Fact] void should_share_between_stores_of_the_same_name() => _other.Metadata.ShouldBeSame(_third.Metadata);
}
