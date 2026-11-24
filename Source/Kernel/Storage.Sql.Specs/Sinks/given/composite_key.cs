// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.given;

public class composite_key(string tenant, string target)
{
    public string Tenant { get; } = tenant;
    public string Target { get; } = target;
}
