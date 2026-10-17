// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Stable persisted names, independent of CLR type names. These values must not change when types are renamed.
/// </summary>
public static class ObserverDefinitionChangeRowFamilies
{
    /// <summary>
    /// The durable step progress family.
    /// </summary>
    public const string Progress = "observer-definition-change-progress";

    /// <summary>
    /// The partition continuation family.
    /// </summary>
    public const string Continuations = "observer-definition-change-continuations";

    /// <summary>
    /// The replay restart-plan family.
    /// </summary>
    public const string RestartPlans = "observer-definition-change-restart-plans";

    /// <summary>
    /// The job inventory and terminal-outcome family.
    /// </summary>
    public const string Inventory = "observer-definition-change-inventory";

    /// <summary>
    /// Gets the stable persisted name of a supported payload type.
    /// </summary>
    /// <typeparam name="T">The row payload.</typeparam>
    /// <returns>The persisted family name.</returns>
    /// <exception cref="UnsupportedObserverDefinitionChangeRowFamily">The payload is not a row family.</exception>
    public static string For<T>() => typeof(T) switch
    {
        var type when type == typeof(ObserverProgressRecord) => Progress,
        var type when type == typeof(ObserverContinuation) => Continuations,
        var type when type == typeof(ObserverRestartPlan) => RestartPlans,
        var type when type == typeof(ObserverJobInventory) => Inventory,
        _ => throw new UnsupportedObserverDefinitionChangeRowFamily(typeof(T))
    };
}
