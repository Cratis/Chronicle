// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// The exception that is thrown when a read model is seeded through <c>Given.ForEventSourceId(...).ReadModel(...)</c>
/// while the <see cref="ReactorScenario{TReactor}"/> was constructed with an explicit <see cref="IEventStore"/> that
/// the scenario does not own — seeding requires the scenario's own in-process event store.
/// </summary>
/// <param name="reactorType">The <see cref="Type"/> of the reactor under test.</param>
public class CannotSeedReadModelWithExplicitEventStore(Type reactorType)
    : Exception(
        $"Cannot seed a read model for the '{reactorType.FullName}' scenario because an explicit IEventStore was supplied. " +
        "Omit the eventStore argument so the scenario provides its own in-process event store, or make the supplied event " +
        "store return the read model itself.");
