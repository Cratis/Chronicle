// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_application_metadata;

public class and_mutating_a_schema : given.application_metadata_sharing
{
    string _originalTitle;

    void Establish()
    {
        _other = new(null, _artifacts);
        _third = new(null, _artifacts);
        _originalTitle = SchemaFor(_other).Title;
    }

    void Because() => SchemaFor(_store).Title = "store-local title";

    [Fact] void should_not_change_the_other_stores_schema() => SchemaFor(_other).Title.ShouldEqual(_originalTitle);
    [Fact] void should_not_change_the_schema_of_a_store_created_afterwards() => SchemaFor(_third).Title.ShouldEqual(_originalTitle);
}
