// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Integration.AccountHolders
{
    public interface IKontoEierSystem
    {
        Task<KontoEier> GetBySocialSecurityNumber(string socialSecurityNumber);
        Task<IEnumerable<KontoEier>> GetBySocialSecurityNumbers(IEnumerable<string> socialSecurityNumbers);
    }
}
