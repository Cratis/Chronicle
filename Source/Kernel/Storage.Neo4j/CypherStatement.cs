// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents a Cypher statement and its parameters.
/// </summary>
/// <remarks>
/// The text only ever holds identifiers that come from the graph definition, escaped with backticks, and parameter
/// references. Every value from an event travels in <see cref="Parameters"/>.
/// </remarks>
/// <param name="Text">The Cypher text.</param>
/// <param name="Parameters">The parameters, by name without the leading <c language="text">$</c>.</param>
public record CypherStatement(string Text, IReadOnlyDictionary<string, object?> Parameters)
{
    /// <summary>
    /// Gets the parameters in the form the driver takes.
    /// </summary>
    /// <returns>The parameters by name.</returns>
    public IDictionary<string, object> ToDriverParameters()
    {
        var parameters = new Dictionary<string, object>(Parameters.Count);
        foreach (var (name, value) in Parameters)
        {
            parameters[name] = value!;
        }

        return parameters;
    }
}
