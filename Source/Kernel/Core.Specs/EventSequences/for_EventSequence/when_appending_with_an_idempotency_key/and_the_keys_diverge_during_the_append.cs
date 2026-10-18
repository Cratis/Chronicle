// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_keys_diverge_during_the_append : given.an_event_sequence_with_idempotency_keys
{
    void Establish()
    {
        _request.Content["name"] = "Alice";
        _request.Content["other"] = "Bob";
        _complianceManager.LocateProtectedValues(Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns([
            new("name", ProtectionKind.Pii, _eventSourceId.Value, _request.Content["name"]!.DeepClone()),
            new("other", ProtectionKind.Pii, _eventSourceId.Value, _request.Content["other"]!.DeepClone())]);
        _complianceManager.ApplyReportingKeys(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns((
            _request.Content.DeepClone().AsObject(), (IReadOnlyList<ProtectedValueUse>)[
                new("name", ProtectionKind.Pii, _request.Content["name"]!.DeepClone(), new(ProtectionKind.Pii, _eventSourceId.Value, _protectingKey, EncryptionKeyRevision.Initial)),
                new("other", ProtectionKind.Pii, _request.Content["other"]!.DeepClone(), new(ProtectionKind.Pii, _eventSourceId.Value, new([7, 8, 9], [10, 11, 12]), EncryptionKeyRevision.Initial))]));
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_refuse_the_divergent_key_set() => _result.Result.Errors.Single().Value.StartsWith("IdempotencyKeyUnsettled:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
