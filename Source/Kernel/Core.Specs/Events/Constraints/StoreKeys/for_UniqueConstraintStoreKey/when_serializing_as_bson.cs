// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintStoreKey;

public class when_serializing_as_bson : given.secret_material
{
    BsonDocument _direct;
    BsonDocument _record;

    void Because()
    {
        _direct = _material.ToBsonDocument();
        _record = _containing.ToBsonDocument();
    }

    [Fact] void should_expose_no_material_directly() => _direct.ElementCount.ShouldEqual(0);
    [Fact] void should_expose_no_material_from_a_containing_record() => _record.Elements.Single(element => element.Name.Equals("Material", StringComparison.OrdinalIgnoreCase)).Value.AsBsonDocument.ElementCount.ShouldEqual(0);
}
