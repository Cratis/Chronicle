// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Namespaces;

/// <summary>
/// The exception that is thrown when the namespace HTTP header carries more than one distinct namespace.
/// </summary>
/// <param name="headerName">The name of the header.</param>
/// <param name="namespaces">The distinct namespaces the header carried.</param>
public class AmbiguousNamespaceHttpHeader(string headerName, IEnumerable<string> namespaces)
    : Exception($"The '{headerName}' header names more than one namespace ({string.Join(", ", namespaces.Select(_ => $"'{_}'"))}). Send the header once, with a single namespace.");
