// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;
using FluentValidation;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the validator for <see cref="RegisterDeployment"/>.
/// </summary>
/// <remarks>
/// Deployments are registered by application deployment clients for their own event store; the System event store is
/// reserved for the kernel.
/// </remarks>
internal class RegisterDeploymentValidator : CommandValidator<RegisterDeployment>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterDeploymentValidator"/> class.
    /// </summary>
    public RegisterDeploymentValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).NotSystemEventStore();
    }
}
