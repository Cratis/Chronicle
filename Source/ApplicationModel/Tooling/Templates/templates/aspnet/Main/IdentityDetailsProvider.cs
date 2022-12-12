// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.Identity;
using Aksio.Cratis.Execution;

namespace Sample;

public class IdentityDetailsProvider : IProvideIdentityDetails
{
    public Task<object> Provide(IdentityProviderContext context)
    {
        object result = new { Value = "Forty two", Tenant = TenantId.Development };
        return Task.FromResult(result);
    }
}
