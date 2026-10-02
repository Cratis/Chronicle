// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants;

public class and_round_tripping_the_matrix(ITestOutputHelper output)
{
    static readonly EncryptionKey _key = new Encryption().GenerateKey();

    public static TheoryData<string, bool, string, string, bool> Cells => given.compliance_matrix.Cells;

    [Theory]
    [MemberData(nameof(Cells))]
    public async Task should_preserve_the_compliance_invariants(string shape, bool erased, string member, string protection, bool pipeline)
    {
        var specimen = given.compliance_matrix.Create(shape, member, protection);
        var keys = new InMemoryEncryptionKeyStorage();
        await keys.SaveFor("store", "Default", "matrix-subject", _key);
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        var compliance = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
        var failures = new HashSet<string>(StringComparer.Ordinal);
        Exception? initialError = null;
        var details = new List<string>();

        var stage = "initial";
        ExpandoObject? stored = null;
        ExpandoObject? released = null;
        await Observe(async () =>
        {
            stored = await Store(specimen.State);
            CheckStored(stored, false);
            CheckReleased(await compliance.Release("store", "Default", specimen.Schema, stored), false);
        });
        if (erased)
        {
            await keys.RecordErasureFor("store", "Default", "matrix-subject");
            await keys.DeleteFor("store", "Default", "matrix-subject");
            stage = "release_erased";
            if (stored is not null)
            {
                await Observe(async () =>
                {
                    released = await compliance.Release("store", "Default", specimen.Schema, stored);
                    CheckReleased(released, true);
                });
            }
            stage = "plaintext_erased";
            if (protection == "pii")
            {
                await Observe(async () =>
                {
                    var plaintext = given.compliance_matrix.Create(shape, member, protection).State;
                    ((IDictionary<string, object?>)plaintext)["__subject"] = "matrix-subject";
                    CheckReleased(await compliance.Release("store", "Default", specimen.Schema, plaintext), true);
                    var converter = new ExpandoObjectConverter(new TypeFormats());
                    var json = converter.ToJsonObject(plaintext, specimen.Schema);
                    json["__subject"] = "matrix-subject";
                    var releasedJson = await compliance.ReleaseJson("store", "Default", specimen.Schema, json);
                    CheckReleased(converter.ToExpandoObject(releasedJson, specimen.Schema), true);
                });
            }
            stage = "fresh_erased";
            await Observe(async () =>
            {
                var updated = await Store(specimen.State);
                CheckStored(updated, true);
                CheckReleased(await compliance.Release("store", "Default", specimen.Schema, updated), true);
            });
            stage = "reapplied_erased";
            if (released is not null)
            {
                await Observe(async () =>
                {
                    var reapplied = await Store(released);
                    CheckStored(reapplied, true, onlyProtection: true);
                    CheckReleased(await compliance.Release("store", "Default", specimen.Schema, reapplied), true);
                });
            }
            Check(!await keys.HasFor("store", "Default", "matrix-subject"), "fence", "no new key after erasure");
        }

        async Task Observe(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception exception)
            {
                initialError ??= exception;
                Check(false, protection == "undeclared" && exception is SchemaPropertyNotFoundInSchema ? "I4_nested" : "apply_or_release", $"{exception.GetType().Name}: {exception.Message}; {exception.InnerException?.GetType().Name}");
            }
        }

        var unexpected = failures.Where(invariant => !given.known_main_limitations.Allows(shape, erased, member, protection, invariant, initialError)).ToArray();
        if (failures.Count > 0) output.WriteLine($"Observed limitations: {string.Join(',', failures.Order(StringComparer.Ordinal))}\n{string.Join('\n', details)}");
        Assert.DoesNotContain(details, _ => _.StartsWith("I1/initial:", StringComparison.Ordinal));
        Assert.DoesNotContain(details, _ => _.StartsWith("I1/fresh_erased:", StringComparison.Ordinal));
        Assert.DoesNotContain(details, _ => _.StartsWith("I1/reapplied_erased:", StringComparison.Ordinal));
        Assert.DoesNotContain(details, _ => _.StartsWith("I2/", StringComparison.Ordinal));
        Assert.True(unexpected.Length == 0, $"INVARIANTS: {string.Join(',', unexpected.Order(StringComparer.Ordinal))}\n{string.Join('\n', details)}");

        void Check(bool holds, string invariant, string detail)
        {
            if (holds) return;
            failures.Add(invariant);
            details.Add($"{invariant}/{stage}: {detail}");
        }

        void CheckStored(ExpandoObject stored, bool isErased, bool onlyProtection = false)
        {
            if (onlyProtection && protection != "pii") return;
            var document = JsonSerializer.SerializeToNode(stored)!.AsObject();
            foreach (var path in specimen.Paths)
            {
                var actual = given.compliance_matrix.At(stored, path);
                var expected = JsonSerializer.SerializeToNode(given.compliance_matrix.MemberValue(member));
                if (protection == "pii")
                {
                    var encrypted = actual is JsonValue scalar && scalar.TryGetValue<string>(out var text) && ProtectedValueCodec.TryDecodeCipherText(encryption, text, out _);
                    var placeholder = JsonSerializer.SerializeToNode(member == "value_object" || member == "nullable_value_object" ? string.Empty : given.compliance_matrix.ErasedValue(member));
                    var protectedOrAbsent = expected is null ? actual is null : encrypted;
                    Check(isErased ? JsonNode.DeepEquals(actual, placeholder) : protectedOrAbsent, "I1", $"{path}: stored {actual?.ToJsonString() ?? "null"}");
                }
                else if (shape == "case_distinct" && protection == "undeclared")
                {
                    Check(!document.ContainsKey(path), "I6", "An undeclared alternate casing must not bypass the declared member");
                }
                else
                {
                    Check(JsonNode.DeepEquals(actual, expected), "I4_member", $"{path}: expected {expected?.ToJsonString() ?? "null"}, stored {actual?.ToJsonString() ?? "null"}");
                    if (expected is null && !path.Contains('.', StringComparison.Ordinal)) Check(document.ContainsKey(path), "I4_null", path);
                }
            }
            if (onlyProtection) return;
            Check(document[specimen.Key]?.GetValue<string>() == "matrix-subject", "I4_key", document[specimen.Key]?.ToJsonString() ?? "missing");
            Check(document["extra"]?.GetValue<string>() == "undeclared-state", "I4_undeclared", document["extra"]?.ToJsonString() ?? "missing");
            Check(document["__initialized"]?.GetValue<bool>() == true && document["__lastHandledEventSequenceNumber"]?.GetValue<long>() == 42, "I4_bookkeeping", "initialization flag or watermark changed");
            if (shape == "case_distinct") Check(JsonNode.DeepEquals(document["Value"], JsonSerializer.SerializeToNode(given.compliance_matrix.MemberValue(member))), "I4_case", document["Value"]?.ToJsonString() ?? "missing");
        }

        void CheckReleased(ExpandoObject released, bool isErased)
        {
            // Main's release conversion only returns declared members; undeclared carry-through is a storage contract.
            if (protection == "undeclared") return;
            foreach (var path in specimen.Paths)
            {
                var expected = JsonSerializer.SerializeToNode(isErased && protection == "pii" ? given.compliance_matrix.ErasedValue(member) : given.compliance_matrix.MemberValue(member));
                var actual = given.compliance_matrix.At(released, path);
                Check(JsonNode.DeepEquals(actual, expected), isErased && protection == "pii" ? "I2" : "I3", $"{path}: expected {expected?.ToJsonString() ?? "null"}, released {actual?.ToJsonString() ?? "null"}");
            }
        }

        async Task<ExpandoObject> Store(ExpandoObject state)
        {
            if (!pipeline) return await compliance.Apply("store", "Default", specimen.Schema, "matrix-subject", state);
            var comparer = new ObjectComparer();
            var projection = Substitute.For<IProjection>();
            projection.TargetReadModelSchema.Returns(specimen.Schema);
            projection.InitialModelState.Returns(state);
            var @event = new AppendedEvent(EventContext.From("store", "Default", EventType.Unknown, EventSourceType.Default, "matrix-subject", EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.NotSet), new ExpandoObject());
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(comparer, @event, new ExpandoObject());
            var context = new ProjectionEventContext(new Key("matrix-subject", ArrayIndexers.NoIndexers), @event, changeset, ProjectionOperationType.None, false);
            var sink = Substitute.For<ISink>();
            context = await new SetInitialState(sink, NullLogger<SetInitialState>.Instance).Perform(projection, context);
            await new EncryptChangeset(compliance, comparer, "store", "Default").Perform(projection, context);
            var differences = changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().SelectMany(change => change.Differences);
            Check(!differences.Any(difference => difference.PropertyPath.ToString() == specimen.Key && difference.Changed is null), "I4_key_difference", "EncryptChangeset must not emit a null primary key");
            return (ExpandoObject)changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().Last().State;
        }
    }
}
