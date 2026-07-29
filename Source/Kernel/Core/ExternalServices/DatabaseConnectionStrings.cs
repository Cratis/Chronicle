// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.DependencyInjection;
using Cratis.Types;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents an implementation of <see cref="IDatabaseConnectionStrings"/>.
/// </summary>
/// <param name="formatters">The discovered <see cref="IFormDatabaseConnectionString"/> formatters.</param>
[Singleton]
public class DatabaseConnectionStrings(IInstancesOf<IFormDatabaseConnectionString> formatters) : IDatabaseConnectionStrings
{
    readonly Dictionary<ExternalServiceEndpointType, IFormDatabaseConnectionString> _formattersByType =
        formatters.ToDictionary(_ => _.Type);

    /// <inheritdoc/>
    public string GetFor(ExternalServiceEndpoint endpoint)
    {
        if (endpoint.Database is null)
        {
            throw new MissingDatabaseConfiguration(endpoint.Type);
        }

        if (!_formattersByType.TryGetValue(endpoint.Type, out var formatter))
        {
            throw new UnsupportedDatabaseEndpointType(endpoint.Type);
        }

        return formatter.Format(endpoint.Database);
    }
}
