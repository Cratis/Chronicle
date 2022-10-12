/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { Address } from './Address';
import { CreditAccount } from './CreditAccount';
import { DebitAccount } from './DebitAccount';
import { IAccount } from './IAccount';

export class AccountHolderWithAccounts {

    @field(String)
    firstName!: string;

    @field(String)
    lastName!: string;

    @field(String)
    socialSecurityNumber!: string;

    @field(Address)
    address!: Address;

    @field(Object, true, [
        CreditAccount,
        DebitAccount
    ])
    accounts!: IAccount[];
}
