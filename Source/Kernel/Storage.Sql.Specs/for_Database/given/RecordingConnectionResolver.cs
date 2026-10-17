// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class RecordingConnectionResolver : IConnectionResolver
{
    public ConcurrentQueue<ConnectionRequest> Requests { get; } = new();

    public Func<ConnectionRequest, ResolvedConnection> Answer { get; set; } = _ => ResolvedConnection.Unchanged;

    public Task<ResolvedConnection> Resolve(ConnectionRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Enqueue(request);
        return Task.FromResult(Answer(request));
    }
}
