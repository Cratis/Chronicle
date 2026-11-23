// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class Sink
{
    /// <inheritdoc/>
    public Task EnsureProvenance() => SinkAcquisitionProvenance.Ensure(new SinkProvenanceStorage(_database, _eventStoreName, _namespace, _tableName));

    Task EnsureDestinationAvailable(ReadModelContainerName containerName) =>
        new SinkProvenanceStorage(_database, _eventStoreName, _namespace, containerName).EnsureDestinationAvailable();
}
