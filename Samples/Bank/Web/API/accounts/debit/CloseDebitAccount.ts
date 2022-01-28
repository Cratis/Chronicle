/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command } from '@aksio/cratis-applications-frontend/commands';

export class CloseDebitAccount extends Command {
    readonly route: string = '/api/accounts/debit/close';

    accountId!: string;
}
