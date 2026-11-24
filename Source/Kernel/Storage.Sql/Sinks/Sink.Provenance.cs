// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class Sink
{
    readonly ConcurrentDictionary<ReadModelContainerName, Task> _availableDestinations = new();
    ISinkProvenanceStorage? _provenance;
    SinkProvenanceStorage? _rawProvenance;

    SinkProvenanceTarget? ProvenanceTarget => _provenance is null ? null :
        _rawProvenance?.Target ?? throw new InvalidOperationException("Read-model storage is not available.");

    /// <inheritdoc/>
    public Task EnsureProvenance()
    {
        _rawProvenance ??= new(_database, _eventStoreName, _namespace, _tableName);
        _provenance ??= _rawProvenance;
        return SinkAcquisitionProvenance.Ensure(_provenance);
    }

    internal void BindProvenance(ISinkProvenanceStorage cached, SinkProvenanceStorage raw)
    {
        _provenance = cached;
        _rawProvenance = raw;
    }

    async Task EnsureDestinationAvailable(ReadModelContainerName containerName)
    {
        SinkProvenanceNames.EnsureNotReserved(containerName);
        if (_provenance is not null) await EnsureProvenance();
        if (string.Equals(containerName.Value, _tableName, StringComparison.Ordinal) && _rawProvenance?.Target is not null) return; // Validated on cold initialization.
        var validation = _availableDestinations.GetOrAdd(containerName, ValidateDestination);
        try { await validation; }
        catch { _availableDestinations.TryRemove(new(containerName, validation)); throw; }
    }

    async Task ValidateDestination(ReadModelContainerName containerName)
    {
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, SinkProvenanceNames.Metadata,
            SinkProvenanceSchema.MarkerColumns, admittedTarget: ProvenanceTarget);
        await SinkProvenanceSchema.EnsureDestinationAvailable(scope.DbContext, containerName);
    }
}
