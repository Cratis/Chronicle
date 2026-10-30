// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace TestAssembly.AdministratorPolicies;

[ReadModel]
public record MethodProtectedAdministrator(Guid UserId)
{
    [Authorize(Policy = "Chronicle.Administrator")]
    public static IEnumerable<MethodProtectedAdministrator> FindAdministrators() => [];

    public static IEnumerable<MethodProtectedAdministrator> UnprotectedQuery() => [];
}
