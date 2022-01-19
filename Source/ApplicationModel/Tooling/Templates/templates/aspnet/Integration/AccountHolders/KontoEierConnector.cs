using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Aksio.Integration;
using AutoMapper;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Projections;
using ObjectsComparer;

namespace Integration.AccountHolders
{
    public class KontoEierConnector
    {
        readonly IKontoEierSystem _externalSystem;
        readonly IImporter _importer;

        public KontoEierConnector(IKontoEierSystem externalSystem, IImporter importer)
        {
            _externalSystem = externalSystem;
            _importer = importer;
        }

        public async Task ImportOne(string socialSecurityNumber)
        {
            var accountHolder = await _externalSystem.GetBySocialSecurityNumber(socialSecurityNumber);
            await _importer.For<AccountHolder, KontoEier>().Apply(accountHolder);
        }

        public async Task ImportAll(IEnumerable<string> socialSecurityNumbers)
        {
            var accountHolders = await _externalSystem.GetBySocialSecurityNumbers(socialSecurityNumbers);
            await _importer.For<AccountHolder, KontoEier>().Apply(accountHolders);
        }
    }
}
