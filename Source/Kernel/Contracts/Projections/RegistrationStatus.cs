// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the state of the projections a registrant has registered.
/// </summary>
[ProtoContract]
public class RegistrationStatus
{
    /// <summary>
    /// Gets or sets the <see cref="RegistrationState"/>: pending while any accepted registration is unapplied, failed when the latest could not be applied, applied otherwise.
    /// </summary>
    [ProtoMember(1)]
    public RegistrationState State { get; set; }

    /// <summary>
    /// Gets or sets the latest revision accepted from the registrant; zero when there is none.
    /// </summary>
    [ProtoMember(2)]
    public long Revision { get; set; }

    /// <summary>
    /// Gets or sets the latest revision that has been applied; zero when there is none.
    /// </summary>
    [ProtoMember(3)]
    public long AppliedRevision { get; set; }

    /// <summary>
    /// Gets or sets why the latest registration could not be applied, when it could not.
    /// </summary>
    [ProtoMember(4)]
    public string? Failure { get; set; }
}
