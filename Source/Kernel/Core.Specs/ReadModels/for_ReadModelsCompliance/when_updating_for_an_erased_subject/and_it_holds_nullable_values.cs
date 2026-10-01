// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_updating_for_an_erased_subject;

public class and_it_holds_nullable_values : given.a_read_model_for_an_erased_subject
{
    protected override string SchemaJson =>
        """
        {
          "type": "object",
          "properties": {
            "nickname": { "type": ["string", "null"], "compliance": [{ "metadataType": "PII", "details": "" }] },
            "age": { "type": ["integer", "null"], "compliance": [{ "metadataType": "PII", "details": "" }] },
            "birthDate": { "type": "string", "format": "date-time?", "compliance": [{ "metadataType": "PII", "details": "" }] },
            "status": { "type": "string" }
          }
        }
        """;

    protected override IEnumerable<string> PersonalValues => ["1815"];

    protected override ExpandoObject CreateState()
    {
        dynamic state = new ExpandoObject();
        state.nickname = null;
        state.age = 1815;
        state.birthDate = new DateTimeOffset(1815, 12, 10, 0, 0, 0, TimeSpan.Zero);
        return state;
    }

    Task Because() => UpdateTwice();

    [Fact] void should_not_fail_the_update() => _exception.ShouldBeNull();
    [Fact] void should_store_the_update() => ValueOf(_storedAfterUpdate, "status").ShouldEqual(UpdatedStatus);
    [Fact] async Task should_not_provision_a_key() => (await _keyStorage.HasFor(EventStore, Namespace, Subject)).ShouldBeFalse();
    [Fact] void should_store_no_personal_data() => StoresNoPersonalValue(_storedAfterUpdate).ShouldBeTrue();
    [Fact] void should_release_the_number_the_same_way_after_the_update() => Serialized(ValueOf(_releasedAfterUpdate, "age")).ShouldEqual(Serialized(ValueOf(_releasedAfterErasure, "age")));
    [Fact] void should_release_the_date_the_same_way_after_the_update() => Serialized(ValueOf(_releasedAfterUpdate, "birthDate")).ShouldEqual(Serialized(ValueOf(_releasedAfterErasure, "birthDate")));

    [Fact]
    void should_keep_the_absent_value_absent()
    {
        ((IDictionary<string, object?>)_releasedAfterUpdate).TryGetValue("nickname", out var nickname);
        nickname.ShouldBeNull();
    }

    [Fact] void should_store_the_same_shape_on_every_update() => Serialized(_storedAfterSecondUpdate).ShouldEqual(Serialized(_storedAfterUpdate));
}
