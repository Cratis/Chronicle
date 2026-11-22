// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.given;

public class a_fenced_history_release : a_release_boundary
{
    protected int _releaseCalls;
    protected int _fenceReads;
    protected int _fenceReadsDuringRelease;
    IEncryptionKeyStorage _keys;

    void Establish()
    {
        _keys = Substitute.For<IEncryptionKeyStorage>();
        _keys.GetErasureForRelease(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), "person")
            .Returns(_ =>
            {
                _fenceReads++;
                return (EncryptionKeyErasure?)null;
            });
        _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), "person", Arg.Any<JsonObject>())
            .Returns(call => ReleaseWithFence(call.Arg<JsonObject>()));
        _metadata.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), "person", Arg.Any<JsonObject>())
            .Returns(call => ReleaseWithFence(call.Arg<JsonObject>()));
        _event = _event with { OriginalContent = "{\"name\":\"ciphertext\"}" };
    }

    protected async Task ReleaseEvent()
    {
        _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);
        _fenceReadsDuringRelease = _fenceReads;
        await ReleaseKeys.GetErasureForRelease(_keys, _event.Context.EventStore, _event.Context.Namespace, "person");
    }

    protected bool HistoryIsReleased() =>
        _result[0].GenerationalContent.Values.Append(_result[0].OriginalContent).Concat(_result[0].Revisions.Select(revision => revision.Content))
            .All(content => content.Contains("Ada Lovelace", StringComparison.Ordinal));

    async Task<JsonObject> ReleaseWithFence(JsonObject content)
    {
        _releaseCalls++;
        await ReleaseKeys.GetErasureForRelease(_keys, _event.Context.EventStore, _event.Context.Namespace, "person");
        var released = (JsonObject)content.DeepClone();
        foreach (var property in released.ToArray())
        {
            property.Value!.GetValue<string>().ShouldEqual("ciphertext");
            released[property.Key] = "Ada Lovelace";
        }

        return released;
    }
}
