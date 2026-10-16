// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Storage.Sql.Connections;

/// <summary>
/// Represents where and how a database context connects.
/// </summary>
/// <param name="ConnectionString">The connection string, with placement and schema applied.</param>
/// <param name="Schema">The schema the tables live in. Empty means the default schema of the database user.</param>
/// <param name="Request">The <see cref="ConnectionRequest"/> the identity is resolved for, if connection resolution is configured.</param>
internal sealed record SqlTarget(string ConnectionString, string Schema, ConnectionRequest? Request);
