// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Extension methods for passing names to catalog queries as parameters, never as part of the command text.
/// </summary>
static class SqlCatalogParameters
{
    /// <summary>
    /// Add a named string parameter to a command.
    /// </summary>
    /// <param name="command">The <see cref="DbCommand"/> to add the parameter to.</param>
    /// <param name="name">The name of the parameter, without the leading at sign.</param>
    /// <param name="value">The value of the parameter.</param>
    internal static void AddName(this DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = $"@{name}";
        parameter.DbType = DbType.String;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
