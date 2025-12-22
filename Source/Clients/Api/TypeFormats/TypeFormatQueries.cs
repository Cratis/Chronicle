// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Api.TypeFormats;

/// <summary>
/// Represents the API for working with type formats.
/// </summary>
[Route("/api/type-formats")]
public class TypeFormatQueries : ControllerBase
{
    readonly ITypeFormats _typeFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeFormatQueries"/> class.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for working with type formats.</param>
    internal TypeFormatQueries(ITypeFormats typeFormats)
    {
        _typeFormats = typeFormats;
    }

    /// <summary>
    /// Get all supported type formats.
    /// </summary>
    /// <returns>Collection of type formats.</returns>
    [HttpGet]
    public IEnumerable<TypeFormat> AllTypeFormats()
    {
        var formats = _typeFormats.GetAllFormats();
        return formats.Select(kvp => new TypeFormat(kvp.Key.Name, kvp.Value));
    }
}
