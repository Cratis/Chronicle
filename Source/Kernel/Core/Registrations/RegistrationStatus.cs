// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The state of what a registrant has registered.
/// </summary>
/// <param name="State">Pending while any accepted registration of the registrant is unapplied, failed when the latest one could not be applied, applied otherwise.</param>
/// <param name="Revision">The latest revision accepted from the registrant; zero when there is none.</param>
/// <param name="AppliedRevision">The latest revision that has been applied; zero when there is none.</param>
/// <param name="Failure">Why the latest registration could not be applied, when it could not.</param>
[GenerateSerializer]
public record RegistrationStatus(
    [property: Id(0)] RegistrationIntentState State,
    [property: Id(1)] long Revision,
    [property: Id(2)] long AppliedRevision,
    [property: Id(3)] string? Failure);
