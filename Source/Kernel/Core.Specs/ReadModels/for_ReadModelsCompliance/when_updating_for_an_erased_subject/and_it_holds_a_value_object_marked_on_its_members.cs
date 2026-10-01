// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_updating_for_an_erased_subject;

/// <summary>
/// The shape the schema generator emits today for a value object marked [PII]: the marker pushed down to every
/// member, each encrypted on its own, including members whose type cannot hold an empty string.
/// </summary>
public class and_it_holds_a_value_object_marked_on_its_members : given.a_read_model_for_an_erased_subject
{
    static readonly Guid _regionId = Guid.Parse("5f1c0b4e-8a43-4d0e-9a51-0c7a3f2e9b11");

    protected override string SchemaJson =>
        """
        {
          "type": "object",
          "properties": {
            "address": {
              "type": "object",
              "properties": {
                "postalCode": { "type": "integer", "format": "int32", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "verified": { "type": "boolean", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "regionId": { "type": "string", "format": "guid", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "movedInAt": { "type": "string", "format": "date-time", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "kind": { "type": "integer", "enum": [0, 1], "x-enumNames": ["Home", "Work"], "compliance": [{ "metadataType": "PII", "details": "" }] }
              }
            },
            "status": { "type": "string" }
          }
        }
        """;

    protected override IEnumerable<string> PersonalValues => ["1337", _regionId.ToString(), "1999"];

    protected override ExpandoObject CreateState()
    {
        dynamic address = new ExpandoObject();
        address.postalCode = 1337;
        address.verified = true;
        address.regionId = _regionId;
        address.movedInAt = new DateTimeOffset(1999, 1, 1, 0, 0, 0, TimeSpan.Zero);
        address.kind = 1;

        dynamic state = new ExpandoObject();
        state.address = address;
        return state;
    }

    Task Because() => UpdateTwice();

    [Fact] void should_not_fail_the_update() => _exception.ShouldBeNull();
    [Fact] void should_store_the_update() => ValueOf(_storedAfterUpdate, "status").ShouldEqual(UpdatedStatus);
    [Fact] async Task should_not_provision_a_key() => (await _keyStorage.HasFor(EventStore, Namespace, Subject)).ShouldBeFalse();
    [Fact] void should_store_no_personal_data() => StoresNoPersonalValue(_storedAfterUpdate).ShouldBeTrue();
    [Fact] void should_keep_the_value_object_shape_at_rest() => ValueOf(_storedAfterUpdate, "address").ShouldBeOfExactType<ExpandoObject>();
    [Fact] void should_release_the_same_value_object_after_the_update() => Serialized(ValueOf(_releasedAfterUpdate, "address")).ShouldEqual(Serialized(ValueOf(_releasedAfterErasure, "address")));
    [Fact] void should_store_the_same_shape_on_every_update() => Serialized(_storedAfterSecondUpdate).ShouldEqual(Serialized(_storedAfterUpdate));
}
