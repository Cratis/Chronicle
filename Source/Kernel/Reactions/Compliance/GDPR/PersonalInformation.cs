// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0060

using Cratis.Compliance.GDPR;
using Cratis.Events;
using Cratis.Kernel.Compliance.GDPR.Events;
using Cratis.Observation;

namespace Cratis.Kernel.Reactions.Compliance.GDPR;

/// <summary>
/// Represents an observer for personal information.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PersonalInformation"/> class.
/// </remarks>
/// <param name="piiManager"><see cref="IPIIManager"/> for working with PII.</param>
[Observer("f0ef19ad-2ea4-4b8e-b93a-a23d176abcfe")]
public class PersonalInformation(IPIIManager piiManager)
{
    /// <summary>
    /// Handles what needs to happen when personal information is deleted for a person.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The context for the event.</param>
    /// <returns>Awaitable task.</returns>
    public Task TestPersonalInformationAdded(PersonalInformationRegistered @event, EventContext context)
    {
        Console.WriteLine(@event);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles what needs to happen when personal information is deleted for a person.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The context for the event.</param>
    /// <returns>Awaitable task.</returns>
    public Task PersonalInformationDeleted(PersonalInformationForPersonDeleted @event, EventContext context) => piiManager.DeleteEncryptionKeyFor(context.EventSourceId.Value);
}
