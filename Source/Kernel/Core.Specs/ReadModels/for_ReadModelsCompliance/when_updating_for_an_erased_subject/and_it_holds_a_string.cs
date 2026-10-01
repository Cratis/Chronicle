// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_updating_for_an_erased_subject;

public class and_it_holds_a_string : given.a_read_model_for_an_erased_subject
{
    protected override string SchemaJson =>
        """
        {
          "type": "object",
          "properties": {
            "name": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
            "status": { "type": "string" }
          }
        }
        """;

    protected override IEnumerable<string> PersonalValues => ["Ada Lovelace"];

    protected override ExpandoObject CreateState()
    {
        dynamic state = new ExpandoObject();
        state.name = "Ada Lovelace";
        return state;
    }

    Task Because() => UpdateTwice();

    [Fact] void should_not_fail_the_update() => _exception.ShouldBeNull();
    [Fact] void should_store_the_update() => ValueOf(_storedAfterUpdate, "status").ShouldEqual(UpdatedStatus);
    [Fact] async Task should_not_provision_a_key() => (await _keyStorage.HasFor(EventStore, Namespace, Subject)).ShouldBeFalse();
    [Fact] void should_store_no_personal_data() => StoresNoPersonalValue(_storedAfterUpdate).ShouldBeTrue();
    [Fact] void should_release_as_erased_before_the_update() => ValueOf(_releasedAfterErasure, "name").ShouldEqual(string.Empty);
    [Fact] void should_release_as_erased_after_the_update() => ValueOf(_releasedAfterUpdate, "name").ShouldEqual(string.Empty);
    [Fact] void should_store_the_same_shape_on_every_update() => Serialized(_storedAfterSecondUpdate).ShouldEqual(Serialized(_storedAfterUpdate));
}
