// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Storage.InMemory.Imports.for_ImportStorage.given;

public class an_import_storage : Specification
{
    protected ImportStorage _storage;

    protected ImportId _importId = ImportId.New();
    protected ImportId _otherImportId = ImportId.New();

    protected static StagedImportChunk Chunk(ImportId importId, int index, int events, int rejected = 0) => new(
        importId,
        index,
        index * 10,
        events,
        Enumerable.Range(0, events - rejected).Select(_ => new StagedImportEvent(
            (index * 10) + _,
            "first+1",
            "Default",
            $"source-{_}",
            "All",
            "Default",
            """{"name":"Alice"}""",
            ["tag"],
            [new StagedImportNamedTag("customer", "42")],
            DateTimeOffset.UnixEpoch,
            null,
            Guid.NewGuid(),
            [new Causation(DateTimeOffset.UnixEpoch, "Legacy", new Dictionary<string, string> { ["source"] = "legacy" })],
            new Identity("subject", "Migrator"),
            $"key-{_}",
            null,
            (ulong)_)).ToArray(),
        Enumerable.Range(0, rejected).Select(_ => new ImportRejection((index * 10) + events - rejected + _, "first+1", ["invalid"])).ToArray());

    void Establish() => _storage = new();

    protected ImportStorage FreshStorage() => _storage;
}
