// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

/// <summary>
/// Helper for constructing a <see cref="ReactorContextValuesBuilder"/> wired with all built-in providers for specifications.
/// </summary>
internal static class ReactorContextValuesBuilders
{
    /// <summary>
    /// Creates a <see cref="ReactorContextValuesBuilder"/> with all built-in reactor context values providers.
    /// </summary>
    /// <returns>A <see cref="ReactorContextValuesBuilder"/> for use in specifications.</returns>
    public static ReactorContextValuesBuilder ForSpecifications() =>
        new(new KnownInstancesOf<IReactorContextValuesProvider>(
        [
            new EventSourceIdValuesProvider(),
            new EventStreamIdValuesProvider(),
            new EventStreamTypeValuesProvider(),
            new EventSourceTypeValuesProvider(),
            new SubjectValuesProvider()
        ]));
}
