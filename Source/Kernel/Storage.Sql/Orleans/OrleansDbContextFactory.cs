// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Orleans;

/// <summary>
/// Represents a factory for creating instances of <see cref="OrleansDbContext"/> at design time for entity framework.
/// </summary>
public class OrleansDbContextFactory : DbContextFactory<OrleansDbContext>;
