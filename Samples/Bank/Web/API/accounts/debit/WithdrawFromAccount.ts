/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/{{accountId}}/withdraw/{{amount}}');

export interface IWithdrawFromAccount {
    accountId?: string;
    amount?: number;
}

export class WithdrawFromAccountValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        accountId: new Validator(),
        amount: new Validator(),
    };
}

export class WithdrawFromAccount extends Command implements IWithdrawFromAccount {
    readonly route: string = '/api/accounts/debit/{{accountId}}/withdraw/{{amount}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new WithdrawFromAccountValidator();

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

    static use(initialValues?: IWithdrawFromAccount): [WithdrawFromAccount, SetCommandValues<IWithdrawFromAccount>] {
        return useCommand<WithdrawFromAccount, IWithdrawFromAccount>(WithdrawFromAccount, initialValues);
    }
}
