// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyFenceUpdates;

/// <summary>
/// The fence and key documents live outside the Chronicle camel-case convention pack, so every field path a filter, update,
/// sort or index uses must be checked against the raw BSON the server's own serialization registration actually stores.
/// </summary>
public class when_matching_query_paths_to_stored_fields : Specification
{
    static readonly HashSet<string> _logicalOperators = new(["$and", "$or", "$nor"], StringComparer.Ordinal);

    BsonDocument _fence;
    BsonDocument _key;
    string[] _fencePaths;
    string[] _keyPaths;

    void Establish() => SpecSerializationSetup.Initialize();

    void Because()
    {
        var registry = BsonSerializer.SerializerRegistry;
        var fences = new RenderArgs<EncryptionKeyErasureForIdentifier>(registry.GetSerializer<EncryptionKeyErasureForIdentifier>(), registry);
        var keys = new RenderArgs<EncryptionKeyForIdentifier>(registry.GetSerializer<EncryptionKeyForIdentifier>(), registry);
        var request = new EncryptionKeyErasureRequest("request", 7u, 2, ["fingerprint"], true);
        var legacy = new EncryptionKeyErasure(0u, [], true);
        var observed = new EncryptionKeyErasure(7u, ["fingerprint"], false, 3, 2, 2, [request], "seal");
        _fence = new EncryptionKeyErasureForIdentifier("subject", 7u, ["fingerprint"], false, 3, 2, 2, [request], "seal").ToBsonDocument();
        _key = new EncryptionKeyForIdentifier(new("subject", 7u), [1], [2], false, 2, "nonce").ToBsonDocument();

        FilterDefinition<EncryptionKeyErasureForIdentifier>[] fenceFilters =
        [
            EncryptionKeyFenceUpdates.Observed("subject", legacy),
            EncryptionKeyFenceUpdates.Observed("subject", observed),
            EncryptionKeyFenceUpdates.HasRequest("request"),
            !EncryptionKeyFenceUpdates.HasRequest("request"),
            Builders<EncryptionKeyErasureForIdentifier>.Filter.Eq(_ => _.NewKeyAllowed, false),
            Builders<EncryptionKeyErasureForIdentifier>.Filter.Eq(_ => _.Seal, null)
        ];
        UpdateDefinition<EncryptionKeyErasureForIdentifier>[] fenceUpdates =
        [
            EncryptionKeyFenceUpdates.Ensure("subject"),
            EncryptionKeyFenceUpdates.Raise(observed, request),
            EncryptionKeyFenceUpdates.Authorize(observed),
            EncryptionKeyFenceUpdates.UpdateRequest(observed, request),
            EncryptionKeyFenceUpdates.Compact(observed)
        ];
        _fencePaths =
        [
            .. fenceFilters.SelectMany(_ => FilterPaths(_.Render(fences), string.Empty)),
            .. fenceUpdates.SelectMany(_ => UpdatePaths(_.Render(fences).AsBsonDocument))
        ];

        FilterDefinition<EncryptionKeyForIdentifier>[] keyFilters =
        [
            EncryptionKeyStorage.Visible("subject"),
            EncryptionKeyStorage.Visible("subject", 3u, observed),
            EncryptionKeyStorage.Covered("subject", request),
            EncryptionKeyStorage.Owned(new(new("subject", 7u), [1], [2], false, 2, "nonce"))
        ];
        _keyPaths =
        [
            .. keyFilters.SelectMany(_ => FilterPaths(_.Render(keys), string.Empty)),
            .. UpdatePaths(Builders<EncryptionKeyForIdentifier>.Update.Set(_ => _.Published, true).Render(keys).AsBsonDocument),
            .. Builders<EncryptionKeyForIdentifier>.Sort.Descending(_ => _.Id.Revision).Render(keys).Names,
            .. EncryptionKeyStorage.Indexes.SelectMany(_ => _.Keys.Render(keys).Names)
        ];
    }

    [Fact] void should_address_the_request_log_by_its_stored_name() => _fencePaths.ShouldContain("requests");
    [Fact] void should_address_the_request_identifier_by_its_stored_path() => _fencePaths.ShouldContain("requests.requestId");
    [Fact] void should_store_the_request_identifier_at_the_addressed_path() => Exists(_fence, "requests.requestId").ShouldBeTrue();
    [Fact] void should_store_the_version_at_the_addressed_path() => Exists(_fence, "version").ShouldBeTrue();
    [Fact] void should_store_the_seal_at_the_addressed_path() => Exists(_fence, "seal").ShouldBeTrue();
    [Fact] void should_store_the_lifecycle_at_the_addressed_path() => Exists(_fence, "lifecycle").ShouldBeTrue();
    [Fact] void should_find_every_fence_query_path_in_the_stored_fence() => _fencePaths.Where(_ => !Exists(_fence, _)).ShouldBeEmpty();
    [Fact] void should_find_every_key_query_path_in_the_stored_key() => _keyPaths.Where(_ => !Exists(_key, _)).ShouldBeEmpty();
    [Fact] void should_address_the_key_revision_inside_the_identifier() => _keyPaths.ShouldContain("_id.revision");

    static IEnumerable<string> FilterPaths(BsonDocument filter, string prefix)
    {
        foreach (var element in filter)
        {
            if (_logicalOperators.Contains(element.Name))
            {
                foreach (var path in element.Value.AsBsonArray.SelectMany(_ => FilterPaths(_.AsBsonDocument, prefix)))
                {
                    yield return path;
                }
            }
            else if (!element.Name.StartsWith('$'))
            {
                var path = prefix + element.Name;
                yield return path;
                foreach (var nested in OperatorPaths(element.Value, path))
                {
                    yield return nested;
                }
            }
        }
    }

    static IEnumerable<string> OperatorPaths(BsonValue value, string path)
    {
        if (value is not BsonDocument operators)
        {
            yield break;
        }

        foreach (var element in operators)
        {
            var nested = element.Name switch
            {
                "$elemMatch" => FilterPaths(element.Value.AsBsonDocument, $"{path}."),
                "$not" => OperatorPaths(element.Value, path),
                _ => []
            };
            foreach (var nestedPath in nested)
            {
                yield return nestedPath;
            }
        }
    }

    static IEnumerable<string> UpdatePaths(BsonDocument update) =>
        update.Where(_ => _.Name.StartsWith('$')).SelectMany(_ => _.Value.AsBsonDocument.Names)
            .Select(_ => string.Join('.', _.Split('.').Where(segment => !segment.StartsWith('$'))));

    static bool Exists(BsonDocument document, string path)
    {
        BsonValue current = document;
        foreach (var segment in path.Split('.'))
        {
            while (current is BsonArray array)
            {
                if (array.Count == 0)
                {
                    return false;
                }

                current = array[0];
            }

            if (current is not BsonDocument container || !container.TryGetValue(segment, out current))
            {
                return false;
            }
        }

        return true;
    }
}
