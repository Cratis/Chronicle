// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance.Events;
using Cratis.Compliance.GDPR;
using Cratis.Events;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Compliance.Domain.GDPR
{
    [Route("/api/compliance/gdpr/pii")]
    public class PII : Controller
    {
        readonly IEventLog _eventLog;
        readonly IPIIManager _piiManager;

        public PII(IEventLog eventLog, IPIIManager piiManager)
        {
            _eventLog = eventLog;
            _piiManager = piiManager;
        }

        [HttpPost]
        public Task CreateAndRegisterKeyFor([FromBody] CreateAndRegisterKeyFor command) => _piiManager.CreateAndRegisterKeyFor(command.Identifier);

        [HttpPost("delete")]
        public Task DeletePIIForPerson([FromBody] DeletePIIForPerson command) => _eventLog.Append(command.PersonId, new PersonalInformationForPersonDeleted());
    }
}
