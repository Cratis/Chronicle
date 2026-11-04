// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the validator for <see cref="EnsureEventStore"/>.
/// </summary>
internal class EnsureEventStoreValidator : CommandValidator<EnsureEventStore>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnsureEventStoreValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the registered event stores.</param>
    /// <remarks>
    /// A registered event store whose name predates the naming rule is grandfathered: its clients can still reconnect
    /// and ensure it. Only new names have to follow the rule. The registry is only read when the rule is broken.
    /// </remarks>
    public EnsureEventStoreValidator(IStorage storage)
    {
        // Name is the event store being created, not a reference to one that must already exist - the
        // cross-cutting EventStoreNameValidator existence check does not apply here.
        RuleFor(_ => _.Name).IgnoreConceptRules().NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Name)
            .MustAsync(async (name, _) => ResourceNames.ForEventStore(name) == ResourceNameViolation.None || await storage.HasEventStore(name))
            .WithMessage(ResourceNames.EventStoreRuleMessage)
            .When(_ => !string.IsNullOrWhiteSpace(_.Name?.Value) && !string.Equals(_.Name.Value, EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase));
        RuleFor(_ => _.Name)
            .Must(name => !string.Equals(name, EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase))
            .WithMessage("The System event store is reserved for the kernel and cannot be created through the public API.");
    }
}
