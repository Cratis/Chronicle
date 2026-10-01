// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_updating_for_an_erased_subject;

/// <summary>
/// Personal data that reaches the read model of an erased subject from somewhere other than their shredded values -
/// mapped from an event member that was not marked [PII], say - can neither be encrypted nor kept. It is replaced
/// by the erased placeholder; the person asked to be forgotten, and the rest of the update still goes through.
/// </summary>
public class and_a_personal_value_arrives_in_the_clear : given.a_read_model_for_an_erased_subject
{
    const string ArrivingName = "Lady Byron";

    ExpandoObject _storedWithArrivingValue;

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

    protected override IEnumerable<string> PersonalValues => ["Ada Lovelace", ArrivingName];

    protected override ExpandoObject CreateState()
    {
        dynamic state = new ExpandoObject();
        state.name = "Ada Lovelace";
        return state;
    }

    async Task Because()
    {
        dynamic state = new ExpandoObject();
        state.name = ArrivingName;
        state.status = UpdatedStatus;
        _storedWithArrivingValue = await _compliance.Apply(EventStore, Namespace, _schema, Subject, state);
    }

    [Fact] void should_store_the_placeholder() => ValueOf(_storedWithArrivingValue, "name").ShouldEqual(string.Empty);
    [Fact] void should_store_the_rest_of_the_update() => ValueOf(_storedWithArrivingValue, "status").ShouldEqual(UpdatedStatus);
    [Fact] void should_store_no_personal_data() => StoresNoPersonalValue(_storedWithArrivingValue).ShouldBeTrue();
    [Fact] async Task should_not_provision_a_key() => (await _keyStorage.HasFor(EventStore, Namespace, Subject)).ShouldBeFalse();
}
