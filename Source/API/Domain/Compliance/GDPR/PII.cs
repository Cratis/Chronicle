// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance.GDPR;
using Cratis.EventSequences;
using Cratis.Kernel.Compliance.GDPR.Events;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Kernel.Domain.Compliance.GDPR;

/// <summary>
/// Represents the domain API for PII.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PII"/> class.
/// </remarks>
/// <param name="eventLog"><see cref="IEventLog"/> for appending to.</param>
/// <param name="piiManager"><see cref="IPIIManager"/> for key management.</param>
[Route("/api/compliance/gdpr/pii")]
public class PII(IEventLog eventLog, IPIIManager piiManager) : ControllerBase
{
    /// <summary>
    /// Create and register a key.
    /// </summary>
    /// <param name="command"><see cref="CreateAndRegisterKeyFor"/> payload.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public Task CreateAndRegisterKeyFor([FromBody] CreateAndRegisterKeyFor command) => piiManager.CreateAndRegisterKeyFor(command.Identifier);

    /// <summary>
    /// Delete PII for a person.
    /// </summary>
    /// <param name="command"><see cref="DeletePIIForPerson"/> payload.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("delete")]
    public Task DeletePIIForPerson([FromBody] DeletePIIForPerson command) => eventLog.Append(command.PersonId, new PersonalInformationForPersonDeleted());
}
