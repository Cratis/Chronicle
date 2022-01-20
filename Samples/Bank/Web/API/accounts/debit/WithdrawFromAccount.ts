/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command } from '@aksio/cratis-applications-frontend/commands';

export class WithdrawFromAccount extends Command {
    readonly route: string = '/api/accounts/debit/withdraw';

    accountId!: string;
    amount!: number;
}
