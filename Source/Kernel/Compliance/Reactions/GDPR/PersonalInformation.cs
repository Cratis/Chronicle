// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance.Events;
using Cratis.Compliance.GDPR;
using Cratis.Events.Observation;
using Cratis.Events.Store;

#pragma warning disable IDE0060

namespace Cratis.Compliance.Reactions.GDPR
{
    /// <summary>
    /// Represents an observer for personal information.
    /// </summary>
    [Observer("f0ef19ad-2ea4-4b8e-b93a-a23d176abcfe")]
    public class PersonalInformation
    {
        readonly IPIIManager _piiManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonalInformation"/> class.
        /// </summary>
        /// <param name="piiManager"><see cref="IPIIManager"/> for working with PII.</param>
        public PersonalInformation(IPIIManager piiManager) => _piiManager = piiManager;

        /// <summary>
        /// Handles what needs to happen when personal information is deleted for a person.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="context">The context for the event.</param>
        /// <returns>Awaitable task.</returns>
        public Task PersonalInformationDeleted(PersonalInformationForPersonDeleted @event, EventContext context) => _piiManager.DeleteEncryptionKeyFor(context.EventSourceId.Value);
    }
}
