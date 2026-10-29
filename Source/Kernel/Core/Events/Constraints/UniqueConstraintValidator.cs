// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintValidator"/> for unique constraints.
/// </summary>
/// <param name="definition">The <see cref="UniqueConstraintDefinition"/> for the validator.</param>
/// <param name="storage">The <see cref="IUniqueConstraintsStorage"/> to use.</param>
public class UniqueConstraintValidator(
    UniqueConstraintDefinition definition,
    IUniqueConstraintsStorage storage) : IConstraintValidator, IHaveUpdateConstraintIndex
{
    /// <inheritdoc/>
    public IConstraintDefinition Definition => definition;

    /// <summary>
    /// Gets whether the storage can apply the index changes of several events in one operation, through <see cref="ApplyIndexChanges"/>.
    /// </summary>
    internal bool CanApplyIndexChangesTogether => storage is IUniqueConstraintsBatchStorage;

    /// <inheritdoc/>
    public bool CanValidate(ConstraintValidationContext context) =>
        definition.EventDefinitions.Any(_ => _.EventTypeId == context.EventTypeId);

    /// <inheritdoc/>
    public IUpdateConstraintIndex GetUpdateFor(ConstraintValidationContext context) => new UniqueConstraintIndexUpdater(definition, context, storage);

    /// <inheritdoc/>
    public async Task<ConstraintValidationResult> Validate(ConstraintValidationContext context)
    {
        var propertiesWithValues = definition.GetPropertiesAndValues(context).ToList();
        if (propertiesWithValues.Count == 0)
        {
            return ConstraintValidationResult.Success;
        }

        var value = propertiesWithValues.GetValue(definition.IgnoreCasing);
        var scopeKey = definition.Scope.BuildScopeKey(context.EventSourceType, context.EventStreamType, context.EventStreamId);
        var (isAllowed, sequenceNumber) = context.BatchClaims?.TryGetPersistedAnswer(definition.Name, scopeKey, value, context.EventSourceId, out var answer) == true
            ? answer
            : await storage.IsAllowed(context.EventSourceId, definition, value, scopeKey);

        // The persisted index does not yet reflect events earlier in the same batch, so also reconcile against
        // the in-batch claims to catch two events in one batch claiming the same value for different sources.
        if (isAllowed && context.BatchClaims is not null)
        {
            // Since the value is already hashed with case handling, we don't need to apply ToLowerInvariant here
            isAllowed = context.BatchClaims.TryClaim(definition.Name, scopeKey, value, context.EventSourceId);
        }

        // The offending value is deliberately absent from the message. A violation message travels back to whoever
        // attempted the append, and a value worth constraining for uniqueness is by its nature identifying. Naming
        // the property is the actionable half - the caller already holds the value it just tried to append.
        //
        // The details carry it instead, keyed per property, so a WithMessage callback can render it if the
        // application wants to. This has to be the property's own value rather than the `value` above: that one is
        // a SHA-256 hash of every constrained property concatenated, computed to keep the value itself out of the
        // index, so it is neither this property's value nor readable. A callback rendering it produced 64
        // characters of hex where the documented example promised the name the caller had just tried to use.
        return isAllowed ?
            ConstraintValidationResult.Success :
            new()
            {
                Violations = propertiesWithValues.Select(pv =>
                    this.CreateViolation(
                        context,
                        sequenceNumber,
                        $"Event '{context.EventTypeId}' on member '{pv.Property}' violated a unique constraint on sequence number {sequenceNumber}",
                        new() { { WellKnownConstraintDetailKeys.PropertyName, pv.Property }, { WellKnownConstraintDetailKeys.PropertyValue, pv.Value } })).ToImmutableList()
            };
    }

    /// <summary>
    /// Reads the persisted index for every claim in a batch at once, so validating each event does not read it again.
    /// </summary>
    /// <param name="contexts">The <see cref="ConstraintValidationContext">contexts</see> of the events in the batch.</param>
    /// <param name="batchClaims">The <see cref="ConstraintBatchClaims"/> of the batch, which keeps the answers.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Nothing writes the index while a batch is validated - the index is written after the batch is appended - so
    /// reading it once up front answers exactly what reading it per event would.
    /// </remarks>
    internal async Task ReadPersistedClaims(IEnumerable<ConstraintValidationContext> contexts, ConstraintBatchClaims batchClaims)
    {
        if (storage is not IUniqueConstraintsBatchStorage batchStorage) return;

        var claims = contexts
            .Where(CanValidate)
            .Select(context => (Context: context, Properties: definition.GetPropertiesAndValues(context).ToList()))
            .Where(_ => _.Properties.Count > 0)
            .Select(_ => (
                ScopeKey: definition.Scope.BuildScopeKey(_.Context.EventSourceType, _.Context.EventStreamType, _.Context.EventStreamId),
                Claim: new UniqueConstraintClaim(_.Context.EventSourceId, _.Properties.GetValue(definition.IgnoreCasing))))
            .Distinct()
            .GroupBy(_ => _.ScopeKey);

        foreach (var scope in claims)
        {
            UniqueConstraintClaim[] inScope = [.. scope.Select(_ => _.Claim)];
            var answers = await batchStorage.IsAllowedMany(inScope, definition, scope.Key);
            for (var index = 0; index < inScope.Length; index++)
            {
                batchClaims.RecordPersistedAnswer(definition.Name, scope.Key, inScope[index].Value, inScope[index].EventSourceId, answers[index]);
            }
        }
    }

    /// <summary>
    /// Applies the index changes of several appended events in one storage operation.
    /// </summary>
    /// <param name="changes">The <see cref="UniqueConstraintIndexChange">changes</see>, in append order.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>The positions of the claims refused because another event source holds the value.</returns>
    internal Task<IReadOnlyCollection<int>> ApplyIndexChanges(IReadOnlyList<UniqueConstraintIndexChange> changes, CancellationToken cancellationToken) =>
        ((IUniqueConstraintsBatchStorage)storage).ApplyChanges(definition, changes, cancellationToken);
}
