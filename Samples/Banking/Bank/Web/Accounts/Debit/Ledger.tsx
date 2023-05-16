// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useParams } from 'react-router-dom';
import { SpecificDebitAccount } from '../../API/accounts/debit/SpecificDebitAccount';

export const Ledger = () => {
    const params = useParams();
    const [account] = SpecificDebitAccount.use({ accountId: params.id! });

    return (
        <>
            <h1>{account.data.name}</h1>
        </>
    );
};
