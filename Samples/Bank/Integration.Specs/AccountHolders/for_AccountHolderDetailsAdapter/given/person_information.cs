// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Specifications.Integration;

namespace Integration.AccountHolders.for_AccountHolderDetailsAdapter.given;

public class person_information : AdapterSpecificationFor<AccountHolder, KontoEier>
{
    protected const string social_security_number = "12345678901";
    protected const string first_name = "Bør";
    protected const string last_name = "Børson";
    protected static DateTime birth_date = new(1873, 3, 17);
    protected const string address = "Langkaia 1";
    protected const string city = "Oslo";
    protected const string postal_code = "0103";
    protected const string country = "Norge";

    protected override IAdapterFor<AccountHolder, KontoEier> CreateAdapter() => new AccountHolderDetailsAdapter();
}
