/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/{{accountId}}/deposit/{{amount}}');

export interface IDepositToAccount {
    accountId?: string;
    amount?: number;
}

export class DepositToAccount extends Command implements IDepositToAccount {
    readonly route: string = '/api/accounts/debit/{{accountId}}/deposit/{{amount}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;

    private _accountId!: string;
    private _amount!: number;

    get requestArguments(): string[] {
        return [
            'accountId',
            'amount',
        ];
    }

    get properties(): string[] {
        return [
            'accountId',
            'amount',
        ];
    }

    get accountId(): string {
        return this._accountId;
    }

    set accountId(value: string) {
        this._accountId = value;
        this.propertyChanged('accountId');
    }
    get amount(): number {
        return this._amount;
    }

    set amount(value: number) {
        this._amount = value;
        this.propertyChanged('amount');
    }

    static use(initialValues?: IDepositToAccount): [DepositToAccount, SetCommandValues<IDepositToAccount>] {
        return useCommand<DepositToAccount, IDepositToAccount>(DepositToAccount, initialValues);
    }
}
