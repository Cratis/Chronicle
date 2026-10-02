// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants.given;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep;

public class when_reindexing_typed_protected_values : given.a_unique_constraint_to_reindex
{
    public static TheoryData<string, bool> Cases => new()
    {
        { "integer", false }, { "integer", true },
        { "boolean", false }, { "boolean", true },
        { "guid", false }, { "guid", true },
        { "date", false }, { "date", true },
        { "decimal", false }, { "decimal", true },
        { "referenced_enum", false }, { "referenced_enum", true },
        { "value_object", false }, { "value_object", true }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task should_skip_erased_placeholders_without_skipping_legitimate_defaults(string member, bool erased)
    {
        var specimen = compliance_matrix.Create("flat", member, "pii", includeGuard: false);
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var source = EventSourceId.New();
        var json = converter.ToJsonObject(compliance_matrix.State(("value", compliance_matrix.ErasedValue(member))), specimen.Schema);
        var stored = await manager.Apply("store", "Default", specimen.Schema, source, json);
        if (erased)
        {
            await keys.RecordErasureFor("store", "Default", source.Value);
            await keys.DeleteFor("store", "Default", source.Value);
        }
        var released = await manager.ReleaseWithStatus("store", "Default", specimen.Schema, source, stored);
        var property = member == "value_object" ? "value.count" : "value";
        _definition = new("SomeConstraint", [new(_eventType.Id, [property])]);
        _validator = new(_definition, _storage);
        await ReindexConstraintsStep.ReindexEvent(_definition, EventFor(source), converter.ToExpandoObject(released.Value, specimen.Schema), _seen, _validator, _storage, released.UnreadablePaths);

        await _storage.Received(1).Remove(source, _definition.Name, Arg.Any<string>());
        await _storage.Received(erased ? 0 : 1).Save(source, _definition.Name, Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>());
    }
}
