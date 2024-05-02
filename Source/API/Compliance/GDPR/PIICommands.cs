// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.API.Compliance.PersonalInformation;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Compliance.GDPR;

/// <summary>
/// Represents the domain API for PII.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PIICommands"/> class.
/// </remarks>
[Route("/api/compliance/gdpr/pii")]
public class PIICommands() : ControllerBase
{
    /// <summary>
    /// Create and register a key.
    /// </summary>
    /// <param name="command"><see cref="CreateAndRegisterKeyFor"/> payload.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public Task CreateAndRegisterKeyFor([FromBody] CreateAndRegisterKeyFor command) => throw new NotImplementedException();

    /// <summary>
    /// Delete PII for a person.
    /// </summary>
    /// <param name="person"><see cref="PersonId"/> to delete for.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("delete/{person}")]
    public Task DeletePIIForPerson([FromRoute] PersonId person) => throw new NotImplementedException();
}
