/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/{{accountId}}/close');

export interface ICloseDebitAccount {
    accountId?: string;
}

export class CloseDebitAccount extends Command implements ICloseDebitAccount {
    readonly route: string = '/api/accounts/debit/{{accountId}}/close';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;

    private _accountId!: string;

    get requestArguments(): string[] {
        return [
            'accountId',
        ];
    }

    get properties(): string[] {
        return [
            'accountId',
        ];
    }

    get accountId(): string {
        return this._accountId;
    }

    set accountId(value: string) {
        this._accountId = value;
        this.propertyChanged('accountId');
    }

    static use(initialValues?: ICloseDebitAccount): [CloseDebitAccount, SetCommandValues<ICloseDebitAccount>] {
        return useCommand<CloseDebitAccount, ICloseDebitAccount>(CloseDebitAccount, initialValues);
    }
}
