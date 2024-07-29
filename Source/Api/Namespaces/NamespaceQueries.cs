// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Api.Namespaces;

/// <summary>
/// Represents the API for working with namespaces.
/// </summary>
[Route("/api/namespaces")]
public class NamespaceQueries : ControllerBase
{
    /// <summary>
    /// Observes all namespaces registered in Cratis.
    /// </summary>
    /// <returns>An observable for observing a collection of <see cref="Namespace"/>.</returns>
    [HttpGet]
    public ISubject<IEnumerable<Namespace>> AllNamespaces() =>
        throw new NotImplementedException();
}
