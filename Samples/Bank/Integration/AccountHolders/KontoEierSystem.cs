// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Integration.AccountHolders
{
    public class KontoEierSystem : IKontoEierSystem
    {
        public Task<KontoEier> GetBySocialSecurityNumber(string socialSecurityNumber) =>
            Task.FromResult(new KontoEier(
                "03050712345",
                "John",
                "Doe",
                new DateTime(2007, 5, 3),
                "Greengrass 42",
                "Paradise City",
                "48321",
                "Themyscira"));
        public Task<IEnumerable<KontoEier>> GetBySocialSecurityNumbers(IEnumerable<string> socialSecurityNumbers) => throw new NotImplementedException();
    }
}
