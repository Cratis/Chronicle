// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_updating_for_an_erased_subject;

/// <summary>
/// Once a later lawful lifecycle is authorized the fence lets a fresh key through, and the read model protects
/// the subject's personal data again instead of storing the erased placeholder.
/// </summary>
public class and_a_new_key_was_authorized : given.a_read_model_for_an_erased_subject
{
    const string NewName = "Ada King";

    ExpandoObject _storedUnderNewKey;
    ExpandoObject _releasedUnderNewKey;

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

    protected override IEnumerable<string> PersonalValues => ["Ada Lovelace", NewName];

    protected override ExpandoObject CreateState()
    {
        dynamic state = new ExpandoObject();
        state.name = "Ada Lovelace";
        return state;
    }

    async Task Because()
    {
        await _keyStorage.AllowNewKeyFor(EventStore, Namespace, Subject);

        dynamic state = new ExpandoObject();
        state.name = NewName;
        state.status = UpdatedStatus;
        _storedUnderNewKey = await _compliance.Apply(EventStore, Namespace, _schema, Subject, state);
        _releasedUnderNewKey = await _compliance.Release(EventStore, Namespace, _schema, _storedUnderNewKey);
    }

    [Fact] async Task should_provision_a_new_key() => (await _keyStorage.HasFor(EventStore, Namespace, Subject)).ShouldBeTrue();
    [Fact] void should_store_the_value_encrypted() => StoresNoPersonalValue(_storedUnderNewKey).ShouldBeTrue();
    [Fact] void should_not_store_the_placeholder() => ValueOf(_storedUnderNewKey, "name").ShouldNotEqual(string.Empty);
    [Fact] void should_release_the_value() => ValueOf(_releasedUnderNewKey, "name").ShouldEqual(NewName);
}
