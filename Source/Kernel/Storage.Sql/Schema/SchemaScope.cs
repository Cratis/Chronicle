// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// The additional stores and namespaces to provision or verify.
/// </summary>
/// <param name="EventStores">Additional event stores.</param>
/// <param name="Namespaces">Additional namespaces, applied to each store.</param>
public record SchemaScope(IReadOnlyList<string> EventStores, IReadOnlyList<string> Namespaces);
