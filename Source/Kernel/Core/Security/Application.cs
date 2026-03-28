// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the read model for a registered OAuth application, providing query access to the applications system store.
/// </summary>
/// <param name="Id">The unique identifier of the application.</param>
/// <param name="ClientId">The OAuth client identifier.</param>
/// <param name="IsActive">Indicates whether the application is active.</param>
/// <param name="CreatedAt">When the application was registered.</param>
/// <param name="LastModifiedAt">When the application was last modified.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Applications)]
public record Application(
    Guid Id,
    string ClientId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt)
{
    /// <summary>
    /// Observes all applications in the system.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to observe applications from.</param>
    /// <returns>An observable subject emitting collections of applications.</returns>
    internal static ISubject<IEnumerable<Application>> AllApplications(IStorage storage) =>
        storage.System.Applications
            .ObserveAll()
            .TransformSubject(apps => apps.Select(ToApplication));

    static Application ToApplication(Storage.Security.Application app) =>
        new(
            (Guid)app.Id,
            (string)app.ClientId,
            true,
            DateTimeOffset.UtcNow,
            null);
}
