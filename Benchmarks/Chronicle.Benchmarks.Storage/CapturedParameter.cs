// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>A copied parameter, retaining PostgreSQL-specific type information when present.</summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Value">The value at capture time.</param>
/// <param name="DbType">The portable database type.</param>
/// <param name="ProviderParameter">The provider-specific copy, when needed.</param>
internal sealed record CapturedParameter(string Name, object? Value, DbType DbType, DbParameter? ProviderParameter);
