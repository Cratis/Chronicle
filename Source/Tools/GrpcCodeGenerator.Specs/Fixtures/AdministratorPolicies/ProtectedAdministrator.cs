// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace TestAssembly.AdministratorPolicies;

[ReadModel]
[Cratis.Arc.Authorization.Authorize(Policy = "Chronicle.Administrator")]
public record ProtectedAdministrator(Guid UserId)
{
    public static IEnumerable<ProtectedAdministrator> AllAdministrators() => [];
}
