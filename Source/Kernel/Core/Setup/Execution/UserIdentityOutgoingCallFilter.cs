// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Execution;

/// <summary>
/// Represents a filter for managing user identity for outgoing calls.
/// </summary>
public class UserIdentityOutgoingCallFilter : IOutgoingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (context.InterfaceName.StartsWith("Cratis"))
        {
            var userSubject = RequestContext.Get(WellKnownKeys.UserIdentity);
            if (userSubject is not null)
            {
                RequestContext.Set(WellKnownKeys.UserIdentity, userSubject);
            }
        }

        await context.Invoke();
    }
}
