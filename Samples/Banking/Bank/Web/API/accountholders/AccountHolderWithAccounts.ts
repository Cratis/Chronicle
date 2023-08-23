/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { IAccount } from './IAccount';
import { CreditAccount } from './CreditAccount';
import { DebitAccount } from './DebitAccount';

export class AccountHolderWithAccounts {

    @field(String)
    id!: string;

    @field(String)
    firstName!: string;

    @field(String)
    lastName!: string;

    @field(Object, true, [
        CreditAccount,
        DebitAccount
    ])
    accounts!: IAccount[];
}
