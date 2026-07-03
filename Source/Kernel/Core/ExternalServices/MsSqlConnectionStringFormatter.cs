// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents a <see cref="IFormDatabaseConnectionString"/> for Microsoft SQL Server.
/// </summary>
public class MsSqlConnectionStringFormatter : IFormDatabaseConnectionString
{
    /// <inheritdoc/>
    public ExternalServiceEndpointType Type => ExternalServiceEndpointType.MsSql;

    /// <inheritdoc/>
    public string Format(DatabaseEndpointConfiguration configuration)
    {
        var server = configuration.Port == DatabasePort.Unspecified
            ? configuration.Host.Value
            : $"{configuration.Host.Value},{configuration.Port.Value}";

        var builder = new StringBuilder()
            .Append($"Server={server};")
            .Append($"Database={configuration.Database.Value};")
            .Append($"User Id={configuration.Username.Value};")
            .Append($"Password={configuration.Password.Value};");

        foreach (var (key, value) in configuration.Options)
        {
            builder.Append($"{key}={value};");
        }

        return builder.ToString();
    }
}
