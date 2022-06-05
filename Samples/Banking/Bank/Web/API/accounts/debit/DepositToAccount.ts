/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/{{accountId}}/deposit/{{amount}}');

export interface IDepositToAccount {
    accountId?: string;
    amount?: number;
}

export class DepositToAccountValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        accountId: new Validator(),
        amount: new Validator(),
    };
}

export class DepositToAccount extends Command<IDepositToAccount> implements IDepositToAccount {
    readonly route: string = '/api/accounts/debit/{{accountId}}/deposit/{{amount}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new DepositToAccountValidator();

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
