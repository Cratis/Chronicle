// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Helpers for determining whether a stored read model instance has been initialized.
/// </summary>
internal static class ReadModelInitialization
{
    /// <summary>
    /// Determines whether a read model instance is an uninitialized placeholder root.
    /// </summary>
    /// <param name="instance">The read model instance to check.</param>
    /// <returns>True only when the initialized marker is explicitly false; missing and legacy non-boolean markers are not placeholders.</returns>
    internal static bool IsUninitialized(ExpandoObject instance) =>
        ((IDictionary<string, object?>)instance).TryGetValue(WellKnownProperties.ReadModelInstanceInitialized, out var initialized) && initialized is false;
}
