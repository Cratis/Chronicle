// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents the read model for a supported type format.
/// </summary>
/// <param name="JsonType">The JSON schema type.</param>
/// <param name="ClrTypeName">Name of the CLR type.</param>
/// <param name="Format">The format string.</param>
/// <remarks>
/// This carries no <c>[BelongsTo]</c>: the formats describe how the workbench renders and edits a schema, and
/// nothing on the gRPC surface asks for them. Adding a service for it would put an operation on the wire that
/// exists only for one screen.
/// <para>
/// It lives in the schemas namespace rather than one named for the area, because <c>TypeFormats</c> is already a
/// type there and a namespace of the same name shadows it everywhere it is used.
/// </para>
/// </remarks>
[ReadModel]
public record TypeFormat(string JsonType, string ClrTypeName, string Format)
{
    /// <summary>
    /// Gets all supported type formats.
    /// </summary>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> to read the formats from.</param>
    /// <returns>A collection of type formats.</returns>
    public static IEnumerable<TypeFormat> AllTypeFormats(ITypeFormats typeFormats) =>
        [.. typeFormats.GetAllFormatsMetadata().Select(_ => new TypeFormat(_.JsonType, _.ClrType.Name, _.Format))];
}
