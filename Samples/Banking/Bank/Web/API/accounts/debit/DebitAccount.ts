/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { AccountHolder } from './AccountHolder';

export class DebitAccount {

    @field(String)
    id!: string;

    @field(String)
    name!: string;

    @field(String)
    accountHolderId!: string;

    @field(AccountHolder)
    accountHolder!: AccountHolder;

    @field(Number)
    balance?: number;

    @field(Boolean)
    hasCard!: boolean;

    @field(Date)
    lastUpdated!: Date;
}
