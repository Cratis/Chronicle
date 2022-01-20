/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command } from '@aksio/cratis-applications-frontend/commands';

export class DepositToAccount extends Command {
    readonly route: string = '/api/accounts/debit/deposit';

    accountId!: string;
    amount!: number;
}
