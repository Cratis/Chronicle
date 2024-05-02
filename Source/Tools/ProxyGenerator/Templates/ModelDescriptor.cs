// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.ProxyGenerator.Templates;

/// <summary>
/// Describes a query for templating purposes.
/// </summary>
/// <param name="Type">Original type.</param>
/// <param name="Name">Name of the model.</param>
/// <param name="Constructor">The JavaScript constructor for the model type.</param>
/// <param name="IsEnumerable">Whether or not the result is an enumerable or not.</param>
/// <param name="IsObservable">Whether or not the type is an observable.</param>
/// <param name="Imports">Additional import statements.</param>
public record ModelDescriptor(
    Type Type,
    string Name,
    string Constructor,
    bool IsEnumerable,
    bool IsObservable,
    IEnumerable<ImportStatement> Imports)
{
    /// <summary>
    /// Represents an empty <see cref="ModelDescriptor"/>.
    /// </summary>
    public static readonly ModelDescriptor Empty = new(null!, string.Empty, string.Empty, false, false, []);
}
