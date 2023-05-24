// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Read.Accounts.Debit;

[Route("/api/accounts/debit/moneylaundry")]
public class PotentialMoneyLaundryCases : Controller
{
    readonly IMongoCollection<PotentialMoneyLaundryCase> _collection;

    public PotentialMoneyLaundryCases(IMongoCollection<PotentialMoneyLaundryCase> collection) => _collection = collection;

    [HttpGet]
    public async Task<IEnumerable<PotentialMoneyLaundryCase>> AllPotentialMoneyLaundryCases()
    {
        var result = await _collection.FindAsync(_ => true);
        return result.ToEnumerable();
    }
}
