using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Projections;

namespace Integration.AccountHolders
{
    public class AccountHolderDetailsAdapter : AdapterFor<AccountHolder, KontoEier>
    {
        public override Func<KontoEier, EventSourceId> KeyResolver => _ => _.Fnr;

        public override void DefineModel(IProjectionBuilderFor<AccountHolder> builder) => builder
            .From<AccountHolderRegistered>(_ => _
                .Set(m => m.FirstName).To(ev => ev.FirstName)
                .Set(m => m.LastName).To(ev => ev.LastName)
                .Set(m => m.DateOfBirth).To(ev => ev.DateOfBirth))
            .From<AccountHolderAddressChanged>(_ => _
                .Set(m => m.Address).To(ev => ev.Address)
                .Set(m => m.City).To(ev => ev.City)
                .Set(m => m.PostalCode).To(ev => ev.PostalCode)
                .Set(m => m.Country).To(ev => ev.Country));

        public override void DefineImport(IImportBuilderFor<AccountHolder, KontoEier> builder)
        {
            builder
                .WithProperties(_ => _.FirstName, _ => _.LastName, _ => _.DateOfBirth)
                .AppendEvent(_ => new AccountHolderRegistered(_.Changeset.Incoming.FirstName, _.Changeset.Incoming.LastName, _.Changeset.Incoming.DateOfBirth))
                .AppendEvent<AccountHolder, KontoEier, AccountHolderRegistered>();

            builder
                .WithProperties(_ => _.Address, _ => _.City, _ => _.PostalCode)
                .AppendEvent<AccountHolder, KontoEier, AccountHolderAddressChanged>();
        }

        public override void DefineImportMapping(IMappingExpression<KontoEier, AccountHolder> builder) => builder
            .ForMember(_ => _.SocialSecurityNumber, _ => _.MapFrom(_ => _.Fnr))
            .ForMember(_ => _.FirstName, _ => _.MapFrom(_ => _.Fornavn))
            .ForMember(_ => _.LastName, _ => _.MapFrom(_ => _.Etternavn))
            .ForMember(_ => _.DateOfBirth, _ => _.MapFrom(_ => _.FodselsDato))
            .ForMember(_ => _.Address, _ => _.MapFrom(_ => _.Adresse))
            .ForMember(_ => _.City, _ => _.MapFrom(_ => _.By))
            .ForMember(_ => _.Country, _ => _.MapFrom(_ => _.Land));
    }
}
