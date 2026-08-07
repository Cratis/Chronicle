// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target;

/// <summary>
/// The shape a Guid-backed root join has in production: the key arrives as the join source's event source id
/// string and the column is stored as a binary UUID. Filtering on the unconverted string matches nothing.
/// </summary>
public class and_the_key_is_a_guid_string_for_a_guid_column : given.a_changeset_converter_over_typed_columns
{
    readonly Guid _key = Guid.NewGuid();

    Exception _error;
    JoinFilterTarget _result;

    void Because() => _error = Catch.Exception(() => _result = _converter.CreateJoinFilterTarget(RootKey(), JoinOn(nameof(given.JoinTargetReadModel.GuidColumn), _key.ToString())));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_filter_on_the_joined_on_column() => _result.Property.ShouldEqual(nameof(given.JoinTargetReadModel.GuidColumn));
    [Fact] void should_compare_against_the_binary_uuid_the_column_holds() => _result.Value.ShouldEqual(new BsonBinaryData(_key, GuidRepresentation.Standard));
}
