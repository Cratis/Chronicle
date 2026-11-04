// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Sequences;
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
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to read the registered namespaces from.</param>
    /// <remarks>
    /// A registered namespace whose name predates the naming rule is grandfathered: its clients can still reconnect
    /// and ensure it. Only new names have to follow the rule. The registry is only read when the rule is broken.
    /// The reserved combination is never grandfathered, as it names the cluster database.
    /// </remarks>
    public EnsureNamespaceValidator(IGrainFactory grainFactory)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _)
            .MustAsync(async (command, _) => ResourceNames.ForNamespace(command.Namespace.Value) == ResourceNameViolation.None || await IsRegistered(grainFactory, command.EventStore, command.Namespace))
            .WithMessage(ResourceNames.NamespaceRuleMessage)
            .When(_ => !string.IsNullOrWhiteSpace(_.Namespace?.Value));
        RuleFor(_ => _)
            .Must(command => ResourceNames.ForCombination(command.EventStore.Value, command.Namespace.Value) == ResourceNameViolation.None)
            .WithMessage(ResourceNames.NamespaceRuleMessage)
            .When(_ => !string.IsNullOrWhiteSpace(_.Namespace?.Value) && !string.IsNullOrWhiteSpace(_.EventStore?.Value));
    }

    static async Task<bool> IsRegistered(IGrainFactory grainFactory, EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        if (string.IsNullOrWhiteSpace(eventStore?.Value))
        {
            return false;
        }

        var registered = await grainFactory.GetGrain<INamespaces>(eventStore.Value).GetAll();
        return registered.Any(_ => string.Equals(_.Value, @namespace.Value, StringComparison.Ordinal));
    }
}
