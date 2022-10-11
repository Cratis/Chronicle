/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { AccountTransaction } from './AccountTransaction';

export class DebitAccountLatestTransactions {
    @field(String)
    id!: string;
    @field(AccountTransaction, true)
    transactions!: AccountTransaction[];
}
