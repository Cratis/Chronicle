// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

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
