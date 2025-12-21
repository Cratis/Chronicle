// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the API for queries related to application.
/// </summary>
[Route("/api/security/client-credentials")]
public class ApplicationQueries : ControllerBase
{
    readonly IApplications _applications;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationQueries"/> class.
    /// </summary>
    /// <param name="applications">The <see cref="IApplications"/> contract.</param>
    internal ApplicationQueries(IApplications applications)
    {
        _applications = applications;
    }

    /// <summary>
    /// Get all application.
    /// </summary>
    /// <returns>A collection of application.</returns>
    [HttpGet]
    public async Task<IEnumerable<Application>> GetApplication() =>
        (await _applications.GetAll()).ToApi();

    /// <summary>
    /// Observes all application.
    /// </summary>
    /// <returns>An observable for observing a collection of application.</returns>
    [HttpGet("observe")]
    public ISubject<IEnumerable<Application>> AllApplication() =>
        _applications.InvokeAndWrapWithTransformSubject(
            token => _applications.ObserveAll(token),
            clients => clients.ToApi());
}
