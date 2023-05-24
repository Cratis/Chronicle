// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Identity;

namespace Sample;

public class IdentityDetailsProvider : IProvideIdentityDetails
{
    public Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var result = new IdentityDetails(true, new { FirstName = "Jane", LastName = "Doe" });
        return Task.FromResult(result);
    }
}
