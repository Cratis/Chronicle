// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the cross-cutting existence check for <see cref="EventStoreName"/>, applied automatically to every
/// command or query that carries one - except where a command's own validator opts out via
/// <see cref="IConceptRuleBuilder{T, TValue}.IgnoreConceptRules"/> because the property names an event store the
/// command itself is creating (e.g. <see cref="EnsureEventStore.Name"/>).
/// </summary>
/// <remarks>
/// Lives beside the <see cref="EventStores"/> commands rather than next to the <see cref="EventStoreName"/> concept
/// itself: <c>Concepts.csproj</c> is a dependency-free primitives project that <c>Storage.csproj</c> references, so
/// this validator - which needs <see cref="IStorage"/> - cannot live there without a circular project reference.
/// </remarks>
public class EventStoreNameValidator : ConceptValidator<EventStoreName>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreNameValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to check for the event store's existence in.</param>
    public EventStoreNameValidator(IStorage storage) => RuleFor(_ => _.Value)
        .MustAsync(async (value, _) => await storage.HasEventStore(value))
        .WithMessage("Event store does not exist.");
}
