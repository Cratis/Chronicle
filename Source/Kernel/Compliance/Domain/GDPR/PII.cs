// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance.Events;
using Aksio.Cratis.Compliance.GDPR;
using Aksio.Cratis.Events;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Compliance.Domain.GDPR
{
    /// <summary>
    /// Represents the domain API for PII.
    /// </summary>
    [Route("/api/compliance/gdpr/pii")]
    public class PII : Controller
    {
        readonly IEventLog _eventLog;
        readonly IPIIManager _piiManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PII"/> class.
        /// </summary>
        /// <param name="eventLog"><see cref="IEventLog"/> for appending to.</param>
        /// <param name="piiManager"><see cref="IPIIManager"/> for key management.</param>
        public PII(IEventLog eventLog, IPIIManager piiManager)
        {
            _eventLog = eventLog;
            _piiManager = piiManager;
        }

        /// <summary>
        /// Create and register a key.
        /// </summary>
        /// <param name="command"><see cref="CreateAndRegisterKeyFor"/> payload.</param>
        /// <returns>Awaitable task.</returns>
        [HttpPost]
        public Task CreateAndRegisterKeyFor([FromBody] CreateAndRegisterKeyFor command) => _piiManager.CreateAndRegisterKeyFor(command.Identifier);

        /// <summary>
        /// Delete PII for a person.
        /// </summary>
        /// <param name="command"><see cref="DeletePIIForPerson"/> payload.</param>
        /// <returns>Awaitable task.</returns>
        [HttpPost("delete")]
        public Task DeletePIIForPerson([FromBody] DeletePIIForPerson command) => _eventLog.Append(command.PersonId, new PersonalInformationForPersonDeleted());
    }
}
