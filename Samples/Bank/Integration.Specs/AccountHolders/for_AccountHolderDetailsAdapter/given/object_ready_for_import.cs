// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Integration.AccountHolders.for_AccountHolderDetailsAdapter.given;

public class object_ready_for_import : person_information
{
    protected KontoEier object_to_import = new(
        social_security_number,
        first_name,
        last_name,
        birth_date,
        address,
        city,
        postal_code,
        country);
}
