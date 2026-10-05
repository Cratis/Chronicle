// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SharedTypeCatalog;

/// <summary>
/// An empty link is <see cref="CoreOwnedReadModel"/>.
/// A custom link is <see cref="CoreOwnedReadModel">custom <b>link text</b></see>.
/// A BCL link is <see cref="System.Xml.Linq.XDocument"/>.
/// A Microsoft link is <see cref="Microsoft.CSharp.RuntimeBinder.Binder"/>.
/// </summary>
/// <seealso cref="CoreOwnedReadModel">related <b>description</b></seealso>
/// <seealso cref="CoreOwnedReadModel"/>
/// <exception cref="CoreOwnedReadModel">An exception <b>description</b> with <see cref="CoreOwnedReadModel">nested text</see>.</exception>
/// <inheritdoc cref="CoreOwnedReadModel" path="/summary"/>
public enum DocumentedFallbackStatus
{
    /// <summary>
    /// The first value.
    /// </summary>
    First = 0
}
