// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// The exception that is thrown when a <see cref="IReactor"/> cannot be activated for a
/// <see cref="ReactorScenario{TReactor}"/> because one or more of its constructor dependencies could not be resolved.
/// </summary>
/// <param name="reactorType">The <see cref="Type"/> of the reactor that failed to activate.</param>
/// <param name="innerException">The underlying activation exception naming the unresolved dependency.</param>
public class CannotActivateReactorForScenario(Type reactorType, Exception innerException)
    : Exception(
        $"Could not activate reactor '{reactorType.FullName}' for the scenario. " +
        "Ensure every constructor dependency is registered in the scenario's Services (or the IServiceProvider supplied " +
        "to it) — for example a substitute for each service; logging is registered by default. See the inner exception " +
        "for the dependency that could not be resolved.",
        innerException);
