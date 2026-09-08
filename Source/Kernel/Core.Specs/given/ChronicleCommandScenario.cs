// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle;

/// <summary>
/// Creates a <see cref="CommandScenario{TCommand}"/> that can actually see the kernel's validators.
/// </summary>
/// <remarks>
/// <para>
/// A scenario left to itself builds its validator lookup over the ambient <c>Types</c> singleton. In a test host
/// that singleton is materialized while Cratis.Fundamentals is still loading, so it snapshots that assembly alone -
/// 144 types, none of them a validator - and every generated type-discovery provider that registers afterwards is
/// missed. The scenario then finds no validator for the command, validation produces nothing, and a command that
/// should be rejected reports success. Specs that assert a rejection catch it; specs that assert success pass for
/// the wrong reason, which is worse.
///
/// Building the lookup over a <c>Types</c> created here instead reads the providers that have registered by the
/// time the spec runs. The scenario keeps a validator lookup that is already registered rather than replacing it,
/// so supplying one is all this takes.
/// </para>
/// </remarks>
public static class ChronicleCommandScenario
{
    /// <summary>
    /// Creates a <see cref="CommandScenario{TCommand}"/> for a command.
    /// </summary>
    /// <typeparam name="TCommand">Type of command the scenario is for.</typeparam>
    /// <returns>The scenario, with validator discovery in place.</returns>
    public static CommandScenario<TCommand> For<TCommand>()
    {
        var scenario = new CommandScenario<TCommand>();
        scenario.Services.AddSingleton<IDiscoverableValidators>(
            new DiscoverableValidators(new global::Cratis.Types.Types()));
        return scenario;
    }
}
