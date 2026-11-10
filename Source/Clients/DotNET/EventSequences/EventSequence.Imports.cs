// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Imports;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    /// <inheritdoc/>
    public async Task<ImportResult> Import(
        IAsyncEnumerable<EventForEventSourceId> events,
        ImportOptions? options = default,
        IProgress<ImportProgress>? progress = default,
        CancellationToken cancellationToken = default)
    {
        ReadProtectionGuard.Append(nameof(Import), "imported events");
        await using var import = await BeginImport(options ?? ImportOptions.Default, cancellationToken);
        using var reporting = Report(import, progress);
        await import.Add(events, cancellationToken);
        return await import.Complete(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ImportResult> Import(
        IEnumerable<EventForEventSourceId> events,
        ImportOptions? options = default,
        IProgress<ImportProgress>? progress = default,
        CancellationToken cancellationToken = default)
    {
        ReadProtectionGuard.Append(nameof(Import), "imported events");
        await using var import = await BeginImport(options ?? ImportOptions.Default, cancellationToken);
        using var reporting = Report(import, progress);
        await import.Add(events, cancellationToken);
        return await import.Complete(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEventImport> BeginImport(ImportOptions? options = default, CancellationToken cancellationToken = default)
    {
        ReadProtectionGuard.Append(nameof(BeginImport), "imported events");
        options ??= ImportOptions.Default;
        cancellationToken.ThrowIfCancellationRequested();
        Contracts.Imports.BeginImportResponse session;
        try
        {
            session = (await _servicesAccessor.Services.Imports.BeginImport(new()
            {
                EventStore = eventStoreName,
                Namespace = @namespace,
                EventSequenceId = eventSequenceId,
                OnInvalid = (Contracts.Imports.OnInvalidEvent)options.OnInvalid,
                ImportId = options.ImportId?.Value
            })).EnsureSuccess();
        }
        catch (global::Grpc.Core.RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.Unimplemented)
        {
            throw new ImportsNotSupported();
        }

        if (session.Error is not null) throw new ImportCouldNotBegin(session.Error);

        return CreateImport(session.ImportId, options, session.MaxChunkEvents, session.MaxChunkBytes, 0, 0);
    }

    /// <inheritdoc/>
    public async Task<IEventImport> BeginImport(ImportId importId, ImportOptions? options = default, CancellationToken cancellationToken = default)
    {
        ReadProtectionGuard.Append(nameof(BeginImport), "imported events");
        cancellationToken.ThrowIfCancellationRequested();
        var progress = (await _servicesAccessor.Services.Imports.GetImport(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            ImportId = importId.Value
        })).Data ?? throw new ImportCouldNotBegin($"Import '{importId}' does not exist.");
        if (progress.EventSequenceId != eventSequenceId.Value)
            throw new ImportCouldNotBegin($"Import '{importId}' imports into event sequence '{progress.EventSequenceId}', not '{eventSequenceId}'.");

        return CreateImport(
            importId,
            (options ?? ImportOptions.Default) with { OnInvalid = (OnInvalidEvent)progress.OnInvalid, ImportId = importId },
            progress.MaxChunkEvents,
            progress.MaxChunkBytes,
            progress.AcknowledgedChunks,
            progress.AcknowledgedEvents);
    }

    static IDisposable? Report(IEventImport import, IProgress<ImportProgress>? progress) =>
        progress is null ? null : import.Progress.Subscribe(progress.Report, _ => { });

    EventImport CreateImport(ImportId importId, ImportOptions options, int maxChunkEvents, int maxChunkBytes, int acknowledgedChunks, long acknowledgedEvents) => new(
        _servicesAccessor.Services.Imports,
        eventStoreName,
        @namespace,
        importId,
        options,
        maxChunkEvents,
        maxChunkBytes,
        ToImportEvent,
        acknowledgedChunks,
        acknowledgedEvents);

    async Task<Contracts.Imports.ImportEvent> ToImportEvent(EventForEventSourceId @event)
    {
        var routing = @event.EventSource is null
            ? null
            : ResolvedEventRouting.Resolve(eventSources, @event.EventSource, @event.EventStream, @event.EventSourceType, @event.EventStreamType);
        var resolved = ResolvedEventRouting.ResolveMetadata(@event, routing);
        var clrType = resolved.Event.GetType();
        ThrowIfUnknownEventType(eventTypes, clrType);
        var eventType = eventTypes.GetEventTypeFor(clrType);
        if (resolved.Generation is { } generation) eventType = eventType with { Generation = generation };

        return new()
        {
            EventSourceId = resolved.EventSourceId,
            EventSourceType = resolved.EventSourceType,
            EventStreamType = resolved.EventStreamType,
            EventStreamId = resolved.EventStreamId,
            EventType = eventType.ToSequencesContract(),
            Content = (await eventSerializer.Serialize(resolved.Event)).ToJsonString(),
            Tags = clrType.GetTags().Concat(resolved.Tags).Distinct().ToList(),
            NamedTags = ResolveTags(resolved.EventSourceId, eventType, resolved.Event, resolved.NamedTags).Select(_ => _.ToSequencesContract()).ToList(),
            Occurred = ToWireOccurred(resolved.Occurred),
            Subject = (resolved.Subject ?? SubjectResolver.ResolveFrom(resolved.Event))?.Value,
            CorrelationId = resolved.CorrelationId?.Value,
            Causation = resolved.Causation is null ? null : [resolved.Causation.ToSequencesContract()],
            CausedBy = resolved.CausedBy?.ToSequencesContract(),
            IdempotencyKey = resolved.IdempotencyKey?.Value,
            EventSource = routing?.EventSource.Value,
            SourceSequenceNumber = resolved.SourceSequenceNumber?.Value
        };
    }
}
