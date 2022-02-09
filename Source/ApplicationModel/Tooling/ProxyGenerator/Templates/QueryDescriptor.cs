// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.ProxyGenerator.Templates
{
    /// <summary>
    /// Describes a query for templating purposes.
    /// </summary>
    /// <param name="Route">API route for the command.</param>
    /// <param name="Name">Name of the command.</param>
    /// <param name="Model">Type of model the query is for.</param>
    /// <param name="IsEnumerable">Whether or not the result is an enumerable or not.</param>
    /// <param name="Imports">Additional import statements.</param>
    /// <param name="Arguments">Arguments for the query.</param>
    public record QueryDescriptor(string Route, string Name, string Model, bool IsEnumerable, IEnumerable<ImportStatement> Imports, IEnumerable<RequestArgumentDescriptor> Arguments);
}
