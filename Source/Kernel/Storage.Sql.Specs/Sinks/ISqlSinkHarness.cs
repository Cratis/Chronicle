// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Provides isolated SQL storage shared by the sinks and inspection contexts in one specification.
/// </summary>
public interface ISqlSinkHarness : ISinkHarness
{
    /// <summary>
    /// Gets the connection string for the specification's database.
    /// </summary>
    string ConnectionString { get; }
}
