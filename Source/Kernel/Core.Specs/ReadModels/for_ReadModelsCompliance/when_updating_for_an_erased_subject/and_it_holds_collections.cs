// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_updating_for_an_erased_subject;

/// <summary>
/// The three ways a collection carries personal data: marked as a whole (one blob), marked on its element type,
/// and objects with a marked member.
/// </summary>
public class and_it_holds_collections : given.a_read_model_for_an_erased_subject
{
    protected override string SchemaJson =>
        """
        {
          "type": "object",
          "properties": {
            "aliases": {
              "type": "array",
              "items": { "type": "string" },
              "compliance": [{ "metadataType": "PII", "details": "" }]
            },
            "emails": {
              "type": "array",
              "items": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] }
            },
            "contacts": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "phone": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
                  "priority": { "type": "integer", "format": "int32", "compliance": [{ "metadataType": "PII", "details": "" }] },
                  "label": { "type": "string" }
                }
              }
            },
            "status": { "type": "string" }
          }
        }
        """;

    protected override IEnumerable<string> PersonalValues => ["Countess", "ada@example.com", "+44 20 7946 0000"];

    protected override ExpandoObject CreateState()
    {
        dynamic contact = new ExpandoObject();
        contact.phone = "+44 20 7946 0000";
        contact.priority = 7;
        contact.label = "home";

        dynamic state = new ExpandoObject();
        state.aliases = new object[] { "Countess of Lovelace" };
        state.emails = new object[] { "ada@example.com" };
        state.contacts = new object[] { contact };
        return state;
    }

    Task Because() => UpdateTwice();

    [Fact] void should_not_fail_the_update() => _exception.ShouldBeNull();
    [Fact] void should_store_the_update() => ValueOf(_storedAfterUpdate, "status").ShouldEqual(UpdatedStatus);
    [Fact] async Task should_not_provision_a_key() => (await _keyStorage.HasFor(EventStore, Namespace, Subject)).ShouldBeFalse();
    [Fact] void should_store_no_personal_data() => StoresNoPersonalValue(_storedAfterUpdate).ShouldBeTrue();
    [Fact] void should_keep_the_unprotected_member_of_an_element() => Serialized(ValueOf(_storedAfterUpdate, "contacts")).ShouldContain("home");
    [Fact] void should_release_the_collection_marked_as_a_whole_the_same_way_after_the_update() => Serialized(ValueOf(_releasedAfterUpdate, "aliases")).ShouldEqual(Serialized(ValueOf(_releasedAfterErasure, "aliases")));
    [Fact] void should_release_the_marked_elements_the_same_way_after_the_update() => Serialized(ValueOf(_releasedAfterUpdate, "emails")).ShouldEqual(Serialized(ValueOf(_releasedAfterErasure, "emails")));
    [Fact] void should_release_the_elements_with_marked_members_the_same_way_after_the_update() => Serialized(ValueOf(_releasedAfterUpdate, "contacts")).ShouldEqual(Serialized(ValueOf(_releasedAfterErasure, "contacts")));
    [Fact] void should_store_the_same_shape_on_every_update() => Serialized(_storedAfterSecondUpdate).ShouldEqual(Serialized(_storedAfterUpdate));
}
