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
    {
        using var span = activitySource.AppendMany();
        span?.Activity?.Tag(_eventSequenceKey.EventStore);
        span?.Activity?.Tag(_eventSequenceKey.Namespace);
        span?.Activity?.Tag(_eventSequenceKey.EventSequenceId);
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
            await PrepareConstraintIndexes(events.Select(@event => @event.EventType.Id));
            if (events.Any(@event => IsGovernedByPendingIndex(@event.EventType.Id)))
            {
                _metrics?.ConstraintIndexRefused();
                return new AppendManyResult { CorrelationId = correlationId, Errors = [AppendError.ConstraintIndexUnavailable] };
            }

            // Share batch claims so events cannot both claim the same unique value before indexes update.
            var batchClaims = new ConstraintBatchClaims();
            var getValidAndCompliantEvents = new List<(EventToAppend Event, Result<(ExpandoObject CompliantEvent, JsonObject CompliantContent, ConstraintValidationContext ConstraintValidationContext), AppendResult> Result)>();
            foreach (var eventToAppend in events)
            {
                var resolvedEventSourceType = await EventSourceResolution.Resolve(EventSourcesStorage, eventToAppend.EventSource, eventToAppend.EventSourceType, eventToAppend.eventStreamType, correlationId);
                if (resolvedEventSourceType.TryGetError(out var eventSourceError))
                {
                    getValidAndCompliantEvents.Add((eventToAppend, eventSourceError));
                    continue;
                }

                var e = eventToAppend with { EventSourceType = resolvedEventSourceType.AsT0 };
                var result = await GetValidAndCompliantEvent(e.EventSourceType, e.EventSourceId, e.eventStreamType, e.eventStreamId, e.EventType, e.Content, correlationId, e.Subject, batchClaims);
                getValidAndCompliantEvents.Add((e, result));
            }

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

            using var concurrencyCheck = BeginConcurrencyValidation(concurrencyScopes);
            var concurrencyViolations = await ConcurrencyValidator.Validate(concurrencyScopes);
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
            logger.ErrorAppendingMany(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, ex);
            return new AppendManyResult
            {
                CorrelationId = correlationId,
                Errors = [new AppendError(ex.Message)]
            };
        }
    }
}
