// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IBatchContainer"/> for MongoDB event sequence events.
/// </summary>
public class EventSequenceBatchContainer : IBatchContainer
{
    readonly IEnumerable<AppendedEvent> _events;
    readonly IDictionary<string, object> _requestContext;

    /// <inheritdoc/>
    public Guid StreamGuid { get; }

    /// <inheritdoc/>
    public string StreamNamespace { get; }

    /// <inheritdoc/>
    public StreamSequenceToken SequenceToken { get; }

    /// <summary>
    /// Gets the microservice identifier.
    /// </summary>
    public MicroserviceId MicroserviceId { get; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public TenantId TenantId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceBatchContainer"/> class.
    /// </summary>
    /// <param name="events">The <see cref="AppendedEvent"/>.</param>
    /// <param name="streamGuid">The identifier of the stream.</param>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the batch is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the batch is for.</param>
    /// <param name="requestContext">The request context.</param>
    public EventSequenceBatchContainer(
        IEnumerable<AppendedEvent> events,
        Guid streamGuid,
        MicroserviceId microserviceId,
        TenantId tenantId,
        IDictionary<string, object> requestContext)
    {
        MicroserviceId = microserviceId;
        TenantId = tenantId;
        _events = events;
        StreamGuid = streamGuid;
        _requestContext = requestContext;
        StreamNamespace = new MicroserviceAndTenant(microserviceId, tenantId);
        if (_events.Any())
        {
            SequenceToken = new EventSequenceNumberToken(_events.First().Metadata.SequenceNumber);
        }
        else
        {
            SequenceToken = new EventSequenceNumberToken();
        }
    }

    /// <inheritdoc/>
    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>() =>
        _events.Select(_ => new Tuple<T, StreamSequenceToken>((T)(object)_, new EventSequenceNumberToken(_.Metadata.SequenceNumber))).ToArray();

    /// <inheritdoc/>
    public bool ImportRequestContext()
    {
        foreach (var (key, value) in _requestContext)
        {
            RequestContext.Set(key, value);
        }
        return true;
    }

    /// <inheritdoc/>
    public bool ShouldDeliver(IStreamIdentity stream, object filterData, StreamFilterPredicate shouldReceiveFunc) => true;
}
