// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_updating_for_an_erased_subject;

public class and_it_holds_nested_objects : given.a_read_model_for_an_erased_subject
{
    protected override string SchemaJson =>
        """
        {
          "type": "object",
          "properties": {
            "profile": {
              "type": "object",
              "properties": {
                "firstName": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "birth": {
                  "type": "object",
                  "properties": {
                    "year": { "type": "integer", "format": "int32", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "city": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] }
                  }
                },
                "tier": { "type": "string" }
              }
            },
            "status": { "type": "string" }
          }
        }
        """;

    protected override IEnumerable<string> PersonalValues => ["Augusta", "1815", "London"];

    protected override ExpandoObject CreateState()
    {
        dynamic birth = new ExpandoObject();
        birth.year = 1815;
        birth.city = "London";

        dynamic profile = new ExpandoObject();
        profile.firstName = "Augusta";
        profile.birth = birth;
        profile.tier = "gold";

        dynamic state = new ExpandoObject();
        state.profile = profile;
        return state;
    }

    Task Because() => UpdateTwice();

    [Fact] void should_not_fail_the_update() => _exception.ShouldBeNull();
    [Fact] void should_store_the_update() => ValueOf(_storedAfterUpdate, "status").ShouldEqual(UpdatedStatus);
    [Fact] async Task should_not_provision_a_key() => (await _keyStorage.HasFor(EventStore, Namespace, Subject)).ShouldBeFalse();
    [Fact] void should_store_no_personal_data() => StoresNoPersonalValue(_storedAfterUpdate).ShouldBeTrue();
    [Fact] void should_keep_the_unprotected_nested_member() => Serialized(ValueOf(_storedAfterUpdate, "profile")).ShouldContain("gold");
    [Fact] void should_release_the_nested_object_the_same_way_after_the_update() => Serialized(ValueOf(_releasedAfterUpdate, "profile")).ShouldEqual(Serialized(ValueOf(_releasedAfterErasure, "profile")));
    [Fact] void should_store_the_same_shape_on_every_update() => Serialized(_storedAfterSecondUpdate).ShouldEqual(Serialized(_storedAfterUpdate));
}
