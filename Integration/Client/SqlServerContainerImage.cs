// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Defines the stable SQL Server image used by the integration fixtures.
/// </summary>
public static class SqlServerContainerImage
{
    /// <summary>
    /// The explicitly versioned SQL Server 2025 image every fixture and CI pre-pull uses;
    /// see the update procedure in Documentation/contributing/integration-tests.md.
    /// Never move to an older major or CU.
    /// </summary>
    public const string Name = "mcr.microsoft.com/mssql/server:2025-CU10-ubuntu-24.04";
}
