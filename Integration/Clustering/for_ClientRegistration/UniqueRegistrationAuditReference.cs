// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// The constraint the client registers.
/// </summary>
public class UniqueRegistrationAuditReference : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) => builder
        .Unique(constraint => constraint.On<RegistrationAuditEvent>(@event => @event.Reference));
}
