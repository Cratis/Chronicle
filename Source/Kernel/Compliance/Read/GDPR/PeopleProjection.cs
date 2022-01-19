// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance.Events;
using Aksio.Cratis.Events.Projections;

namespace Aksio.Cratis.Compliance.Read.GDPR
{
    /// <summary>
    /// Defines the projection for <see cref="Person"/>.
    /// </summary>
    [Projection("deae7ac0-7eac-48c2-a10a-10aef4e4c02f")]
    public class PeopleProjection : IProjectionFor<Person>
    {
        /// <inheritdoc/>
        public void Define(IProjectionBuilderFor<Person> builder) => builder
            .From<PersonRegistered>(_ => _
                .Set(m => m.SocialSecurityNumber).To(e => e.SocialSecurityNumber)
                .Set(m => m.FirstName).To(e => e.FirstName)
                .Set(m => m.LastName).To(e => e.LastName))
            .From<AddressRegisteredForPerson>(_ => _
                .Set(m => m.Address).To(e => e.Address)
                .Set(m => m.City).To(e => e.City)
                .Set(m => m.PostalCode).To(e => e.PostalCode)
                .Set(m => m.Country).To(e => e.Country))
            .Children(_ => _.PersonalInformation, c => c
                .IdentifiedBy(_ => _.Identifier)
                .From<PersonalInformationRegistered>(_ => _
                    .UsingKey(_ => _.Identifier)
                    .Set(m => m.Type).To(e => e.Type)
                    .Set(m => m.Value).To(e => e.Value)));
    }
}
