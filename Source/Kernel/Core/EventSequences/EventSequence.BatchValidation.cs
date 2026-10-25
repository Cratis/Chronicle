// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Schemas;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    async Task<AppendManyResult> AppendManyValidated(
        IEnumerable<EventToAppend> events,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        ConcurrencyScopes concurrencyScopes,
        Occurrences.EffectReceipt? effectReceipt = null)
        => await AppendManyValidated(events, correlationId, causation, causedBy, new ConcurrencyScopeConjunctions(concurrencyScopes.Scopes.Select(_ => (_.Key, _.Value, (uint?)null))), effectReceipt, concurrencyScopes);

    async Task<AppendManyResult> AppendManyValidated(
        IEnumerable<EventToAppend> events,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        ConcurrencyScopeConjunctions concurrencyScopes,
        Occurrences.EffectReceipt? effectReceipt = null,
        ConcurrencyScopes? legacyScopes = null)
    {
        using var span = activitySource.AppendMany();
        span?.Activity?.Tag(_eventSequenceKey.EventStore);
        span?.Activity?.Tag(_eventSequenceKey.Namespace);
        span?.Activity?.Tag(_eventSequenceKey.EventSequenceId);
        try
        {
            var result = await AppendManyValidatedCore(events, correlationId, causation, causedBy, concurrencyScopes, effectReceipt, legacyScopes);
            span?.Activity?.RecordAppendOutcome(result);
            return result;
        }
        catch (Exception ex)
        {
            span?.Activity?.RecordFailure(ex);
            throw;
        }
    }

    async Task<AppendManyResult> AppendManyValidatedCore(
        IEnumerable<EventToAppend> events,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        ConcurrencyScopeConjunctions concurrencyScopes,
        Occurrences.EffectReceipt? effectReceipt,
        ConcurrencyScopes? legacyScopes)
    {
        try
        {
            if (effectReceipt is not null)
            {
                var existing = await FindEffectReceipt(effectReceipt, correlationId);
                if (existing is not null) return existing;
                ValidateEffectReceipt(effectReceipt);
            }

            await RefreshConstraintsIfChanged();
            events = events as IList<EventToAppend> ?? events.ToList();
            if (await RefusedByPendingConstraintIndex(events.Select(@event => @event.EventType.Id).ToList()))
            {
                return new AppendManyResult { CorrelationId = correlationId, Errors = [AppendError.ConstraintIndexUnavailable] };
            }

            // Share batch claims so events cannot both claim the same unique value before indexes update.
            var batchClaims = new ConstraintBatchClaims();
            var getValidAndCompliantEvents = await ValidateBatchEvents(events, correlationId, batchClaims);

            var failedEvents = getValidAndCompliantEvents.Where(eventAndResult => !eventAndResult.Result.IsSuccess).ToList();

            if (failedEvents.Count != 0)
            {
                return new()
                {
                    CorrelationId = correlationId,
                    ConstraintViolations = failedEvents.SelectMany(r => r.Result.AsT1.ConstraintViolations).ToImmutableList(),
                    Errors = failedEvents.SelectMany(r => r.Result.AsT1.Errors).ToImmutableList(),
                };
            }

            using var concurrencyCheck = legacyScopes is null ? BeginConcurrencyValidation(concurrencyScopes) : BeginConcurrencyValidation(legacyScopes);
            var concurrencyViolations = legacyScopes is null ? await ConcurrencyValidator.Validate(concurrencyScopes) : await ConcurrencyValidator.Validate(legacyScopes);
            if (concurrencyViolations.Any())
            {
                return AppendManyResult.Failed(correlationId, concurrencyViolations).ReportingConcurrencyCheck(concurrencyScopes.ShouldAllBeValidated);
            }

            var identity = await IdentityStorage.GetFor(causedBy.WithoutDuplicates());
            var validatedEvents = getValidAndCompliantEvents.ConvertAll(eventAndResult =>
            {
                var (compliantEvent, compliantContent, constraintContext) = eventAndResult.Result.AsT0;
                return (eventAndResult.Event, CompliantEvent: compliantEvent, CompliantContent: compliantContent, ConstraintContext: constraintContext);
            });

            var appendManyResult = await AppendManyToStorage(validatedEvents, correlationId, causation, identity, effectReceipt);
            return appendManyResult.ReportingConcurrencyCheck(concurrencyScopes.ShouldAllBeValidated);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Activity.Current?.RecordFailure(ex);
            logger.ErrorAppendingMany(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, ex);
            return new AppendManyResult
            {
                CorrelationId = correlationId,
                Errors = [new AppendError(ex.Message)]
            };
        }
    }

    async Task<List<(EventToAppend Event, Result<(ExpandoObject CompliantEvent, JsonObject CompliantContent, ConstraintValidationContext ConstraintValidationContext), AppendResult> Result)>> ValidateBatchEvents(
        IEnumerable<EventToAppend> events,
        CorrelationId correlationId,
        ConstraintBatchClaims batchClaims,
        List<ProtectedValueUse>?[]? keyUses = null)
    {
        var validated = new List<(EventToAppend Event, Result<(ExpandoObject CompliantEvent, JsonObject CompliantContent, ConstraintValidationContext ConstraintValidationContext), AppendResult> Result)>();
        foreach (var (eventToAppend, index) in events.Select((@event, index) => (@event, index)))
        {
            var resolvedEventSourceType = await EventSourceResolution.Resolve(EventSourcesStorage, eventToAppend.EventSource, eventToAppend.EventSourceType, eventToAppend.eventStreamType, correlationId);
            if (resolvedEventSourceType.TryGetError(out var eventSourceError))
            {
                validated.Add((eventToAppend, eventSourceError));
                continue;
            }

            var e = eventToAppend with { EventSourceType = resolvedEventSourceType.AsT0 };
            var result = await GetValidAndCompliantEvent(e.EventSourceType, e.EventSourceId, e.eventStreamType, e.eventStreamId, e.EventType, e.Content, correlationId, e.Subject, batchClaims, keyUses?[index]);
            validated.Add((e, result));
        }

        return validated;
    }
}
