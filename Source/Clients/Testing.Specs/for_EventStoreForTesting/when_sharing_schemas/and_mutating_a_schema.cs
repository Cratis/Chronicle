// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_mutating_a_schema : given.schema_sharing
{
    string _originalTitle;
    string _originalPropertyTitle;

    void Establish()
    {
        _other = new(null, _artifacts);
        _schema = SchemaFor(_store);
        _otherSchema = SchemaFor(_other);
        _originalTitle = _otherSchema.Title;
        _originalPropertyTitle = _otherSchema.Properties["value"].Title;
    }

    void Because()
    {
        _schema.Title = "store-local title";
        _schema.Properties["value"].Title = "store-local property title";
    }

    [Fact] void should_not_change_the_other_stores_title() => _otherSchema.Title.ShouldEqual(_originalTitle);
    [Fact] void should_not_change_the_other_stores_property() => _otherSchema.Properties["value"].Title.ShouldEqual(_originalPropertyTitle);
    [Fact] void should_not_change_the_cached_schema() => GeneratorFor(_store).Generate(typeof(StoreIdentityRecorded)).Title.ShouldEqual(_originalTitle);
}
