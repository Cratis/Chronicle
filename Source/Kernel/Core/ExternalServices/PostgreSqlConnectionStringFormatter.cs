// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents a <see cref="IFormDatabaseConnectionString"/> for PostgreSQL.
/// </summary>
public class PostgreSqlConnectionStringFormatter : IFormDatabaseConnectionString
{
    /// <inheritdoc/>
    public ExternalServiceEndpointType Type => ExternalServiceEndpointType.PostgreSql;

    /// <inheritdoc/>
    public string Format(DatabaseEndpointConfiguration configuration)
    {
        var port = configuration.Port == DatabasePort.Unspecified
            ? string.Empty
            : $"Port={configuration.Port.Value};";

        var builder = new StringBuilder()
            .Append($"Host={configuration.Host.Value};")
            .Append(port)
            .Append($"Database={configuration.Database.Value};")
            .Append($"Username={configuration.Username.Value};")
            .Append($"Password={configuration.Password.Value};");

        foreach (var (key, value) in configuration.Options)
        {
            builder.Append($"{key}={value};");
        }

        return builder.ToString();
    }
}
