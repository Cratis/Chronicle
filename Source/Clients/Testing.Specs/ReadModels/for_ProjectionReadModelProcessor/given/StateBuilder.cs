// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

/// <summary>
/// Builds projection state objects for specs.
/// </summary>
static class StateBuilder
{
    /// <summary>
    /// Builds an <see cref="ExpandoObject"/> holding the given properties.
    /// </summary>
    /// <param name="properties">The name and value pairs to set.</param>
    /// <returns>The populated object.</returns>
    public static ExpandoObject Expando(params (string Name, object? Value)[] properties)
    {
        var expando = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)expando;
        foreach (var (name, value) in properties)
        {
            dictionary[name] = value;
        }

        return expando;
    }

    /// <summary>
    /// Gets the value of a property from a state object.
    /// </summary>
    /// <param name="state">The state object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property value.</returns>
    public static object? Get(object state, string name) => ((IDictionary<string, object?>)state)[name];

    /// <summary>
    /// Gets the items of a collection property from a state object.
    /// </summary>
    /// <param name="state">The state object.</param>
    /// <param name="name">The collection property name.</param>
    /// <returns>The collection items.</returns>
    public static List<object> Items(object state, string name) => ((IEnumerable<object>)Get(state, name)!).ToList();
}
