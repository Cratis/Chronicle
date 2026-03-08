// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Setup.Execution;

/// <summary>
/// Represents a filter for managing user identity for incoming calls.
/// </summary>
public class UserIdentityIncomingCallFilter : IIncomingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (RequestContext.Get(WellKnownKeys.UserIdentity) is string userSubject)
        {
            Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userSubject)]));
        }

        await context.Invoke();
    }
}
