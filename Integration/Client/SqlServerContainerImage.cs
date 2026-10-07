// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Defines the stable SQL Server image used by the integration fixtures.
/// </summary>
public static class SqlServerContainerImage
{
    /// <summary>
    /// The pinned image; the 2025-latest image crashed during startup in CI.
    /// </summary>
    public const string Name = "mcr.microsoft.com/mssql/server:2022-CU20-ubuntu-22.04";
}
