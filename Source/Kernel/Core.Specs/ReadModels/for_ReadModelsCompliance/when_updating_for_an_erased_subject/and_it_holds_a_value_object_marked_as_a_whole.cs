// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_updating_for_an_erased_subject;

/// <summary>
/// The shape a schema stored before metadata was pushed to the leaves still carries: [PII] on the value object
/// itself, encrypted as one value. Its erased release is an empty object that the converter fills with the
/// defaults of its non-nullable members - so the next update hands the handler a value that is neither empty nor
/// personal, and nothing about its shape says it came from an erasure.
/// </summary>
public class and_it_holds_a_value_object_marked_as_a_whole : given.a_read_model_for_an_erased_subject
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
                "postalCode": { "type": "integer", "format": "int32" },
                "verified": { "type": "boolean" },
                "regionId": { "type": "string", "format": "guid" },
                "movedInAt": { "type": "string", "format": "date-time" },
                "kind": { "type": "integer", "enum": [0, 1], "x-enumNames": ["Home", "Work"] }
              },
              "compliance": [{ "metadataType": "PII", "details": "" }]
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

    [Fact] void should_release_as_defaults_before_the_update() => Serialized(ValueOf(_releasedAfterErasure, "address")).ShouldContain("\"postalCode\":0");
    [Fact] void should_not_fail_the_update() => _exception.ShouldBeNull();
    [Fact] void should_store_the_update() => ValueOf(_storedAfterUpdate, "status").ShouldEqual(UpdatedStatus);
    [Fact] async Task should_not_provision_a_key() => (await _keyStorage.HasFor(EventStore, Namespace, Subject)).ShouldBeFalse();
    [Fact] void should_store_no_personal_data() => StoresNoPersonalValue(_storedAfterUpdate).ShouldBeTrue();
    [Fact] void should_store_no_value_from_the_defaults() => Serialized(ValueOf(_storedAfterUpdate, "address")).ShouldNotContain("postalCode");
    [Fact] void should_release_the_same_value_object_after_the_update() => Serialized(ValueOf(_releasedAfterUpdate, "address")).ShouldEqual(Serialized(ValueOf(_releasedAfterErasure, "address")));
    [Fact] void should_store_the_same_shape_on_every_update() => Serialized(_storedAfterSecondUpdate).ShouldEqual(Serialized(_storedAfterUpdate));
}
