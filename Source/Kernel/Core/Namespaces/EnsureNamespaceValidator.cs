// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents the validator for <see cref="EnsureNamespace"/>.
/// </summary>
internal class EnsureNamespaceValidator : CommandValidator<EnsureNamespace>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnsureNamespaceValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to check the event store's existence in.</param>
    /// <remarks>
    /// The command creates the namespace but never the event store it belongs to, so the event store has to be
    /// there already. The check is stated here rather than on the concept: Arc discovers a
    /// <see cref="Cratis.Arc.Validation.ConceptValidator{T}"/> and runs it against every occurrence of that concept
    /// in the model graph, and a nested validator only ever sees the value - never the command that carried it - so
    /// a command that creates an event store would have no way to exempt itself.
    /// </remarks>
    public EnsureNamespaceValidator(IStorage storage)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.")
            .MustAsync(async (eventStore, _) => await storage.HasEventStore(eventStore))
            .WithMessage("Event store does not exist.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
    }
}
