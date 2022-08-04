// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.ProxyGenerator.Templates;

/// <summary>
/// Describes an import statement.
/// </summary>
public record ImportStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportStatement"/> class.
    /// </summary>
    /// <param name="type">Type to use.</param>
    /// <param name="module">Source module the type is originating from.</param>
    public ImportStatement(string type, string module)
    {
        Type = type;
        Module = module.Replace('\\', '/');
    }

    /// <summary>
    /// Gets the type to use.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the source module in which the type is from.
    /// </summary>
    public string Module { get; }
}
