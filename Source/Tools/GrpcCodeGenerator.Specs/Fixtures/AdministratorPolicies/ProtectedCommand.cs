// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestAssembly.AdministratorPolicies;

[Command]
[Authorize(Policy = "Chronicle.Administrator")]
public record ProtectedCommand(Guid UserId)
{
    public void Handle()
    {
    }
}
