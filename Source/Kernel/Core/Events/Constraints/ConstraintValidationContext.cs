// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents the context for constraint validation.
/// </summary>
public record ConstraintValidationContext
{
    readonly (IConstraintValidator Validator, IUpdateConstraintIndex Updater)[] _indexUpdates;
    readonly IEnumerable<IObserveConstraintBatchEvents> _batchObservers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConstraintValidationContext"/> class.
    /// </summary>
    /// <param name="validators">The <see cref="IConstraintValidator">validators</see> involved in the context.</param>
    /// <param name="eventSourceId">The <see cref="Concepts.Events.EventSourceId"/> to validate for.</param>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> to validate for.</param>
    /// <param name="content">The content of the event.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> of the event being validated.</param>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> of the event being validated.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> of the event being validated.</param>
    /// <param name="batchClaims">Optional <see cref="ConstraintBatchClaims"/> shared by all events in a batch append, used to detect intra-batch duplicates.</param>
    public ConstraintValidationContext(
        IEnumerable<IConstraintValidator> validators,
        EventSourceId eventSourceId,
        EventTypeId eventTypeId,
        ExpandoObject content,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        ConstraintBatchClaims? batchClaims = default)
    {
        EventSourceId = eventSourceId;
        EventTypeId = eventTypeId;
        Content = content;
        EventSourceType = eventSourceType;
        EventStreamType = eventStreamType;
        EventStreamId = eventStreamId;
        BatchClaims = batchClaims;
        _indexUpdates = validators.Where(validator => validator is IHaveUpdateConstraintIndex)
            .Select(validator => (validator, ((IHaveUpdateConstraintIndex)validator).GetUpdateFor(this))).ToArray();
        Validators = validators.Where(_ => _.CanValidate(this)).ToArray();

        _batchObservers = batchClaims is not null ? validators.OfType<IObserveConstraintBatchEvents>().ToArray() : [];
    }

    /// <summary>
    /// Gets the <see cref="EventSourceId"/> to validate for.
    /// </summary>
    public EventSourceId EventSourceId { get; }

    /// <summary>
    /// Gets the <see cref="EventTypeId"/> to validate for.
    /// </summary>
    public EventTypeId EventTypeId { get; }

    /// <summary>
    /// Gets the content of the event.
    /// </summary>
    public ExpandoObject Content { get; }

    /// <summary>
    /// Gets the <see cref="EventSourceType"/> of the event being validated.
    /// </summary>
    public EventSourceType? EventSourceType { get; }

    /// <summary>
    /// Gets the <see cref="EventStreamType"/> of the event being validated.
    /// </summary>
    public EventStreamType? EventStreamType { get; }

    /// <summary>
    /// Gets the <see cref="EventStreamId"/> of the event being validated.
    /// </summary>
    public EventStreamId? EventStreamId { get; }

    /// <summary>
    /// Gets the <see cref="IConstraintValidator">validators</see> involved in the context.
    /// </summary>
    public IEnumerable<IConstraintValidator> Validators { get; }

    /// <summary>
    /// Gets the independently addressable index updates and their owning validators.
    /// </summary>
    public IEnumerable<(IConstraintValidator Validator, IUpdateConstraintIndex Updater)> IndexUpdates => _indexUpdates;

    /// <summary>
    /// Gets the <see cref="ConstraintBatchClaims"/> shared by all events in a batch append, or <see langword="null"/> when validating a single event.
    /// </summary>
    public ConstraintBatchClaims? BatchClaims { get; }

    /// <summary>
    /// Gets the <see cref="Concepts.Events.Subject"/> the event's personal data belongs to, if known.
    /// </summary>
    public Subject? Subject { get; init; }

    /// <summary>
    /// Gets the top-level properties of the event that carry personal data according to its schema.
    /// </summary>
    public IReadOnlyCollection<string> PersonalProperties { get; init; } = [];

    /// <summary>
    /// Perform validation on a <see cref="EventToValidateForConstraints"/>.
    /// </summary>
    /// <returns><see cref="ConstraintValidationResult"/> holding the result of validation.</returns>
    public async Task<ConstraintValidationResult> Validate()
    {
        var results = await Task.WhenAll(Validators.Select(v => v.Validate(this)));
        var result = new ConstraintValidationResult
        {
            Violations = results.Where(r => !r.IsValid).SelectMany(r => r.Violations).ToImmutableList()
        };
        if (result.IsValid)
        {
            // A covered event can also be a removal. Validate against preceding events first, then let its
            // release supersede its own claim for subsequent events, even if no validator covers this event.
            foreach (var observer in _batchObservers)
            {
                observer.RecordBatchEvent(this);
            }
        }

        return result;
    }

    /// <summary>
    /// Update constraints with information from the <see cref="EventToValidateForConstraints"/> and <see cref="EventSequenceNumber"/>.
    /// </summary>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> of the event that might affect a constraint.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The purpose of this method is to update any constraints that might be affected by typically appending an event.
    /// Some constraints use this information to keep track of the sequence number that holds information the constraint will use when violated.
    /// </remarks>
    public async Task Update(EventSequenceNumber eventSequenceNumber)
    {
        await Task.WhenAll(_indexUpdates.Select(update => update.Updater.Update(eventSequenceNumber)));
    }

    /// <summary>
    /// Attempts every updater independently and reports every result within its cancellation deadline.
    /// </summary>
    /// <param name="eventSequenceNumber">The event's position.</param>
    /// <param name="cancellationToken">The enclosing turn's cancellation deadline.</param>
    /// <returns>One outcome per updater, including failures and abandoned attempts.</returns>
    public async Task<IEnumerable<ConstraintIndexUpdateResult>> Update(EventSequenceNumber eventSequenceNumber, CancellationToken cancellationToken) =>
        await Task.WhenAll(_indexUpdates.Select(async update =>
        {
            var (outcome, error) = await ConstraintIndexUpdateAttempt.Run(token => update.Updater.Update(eventSequenceNumber, token), cancellationToken);
            return new ConstraintIndexUpdateResult(update.Updater, outcome, error);
        }));
}
