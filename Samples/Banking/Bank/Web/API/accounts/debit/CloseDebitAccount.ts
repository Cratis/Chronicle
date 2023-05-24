/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/{{accountId}}/close');

export interface ICloseDebitAccount {
    accountId?: string;
}

export class CloseDebitAccountValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        accountId: new Validator(),
    };
}

export class CloseDebitAccount extends Command<ICloseDebitAccount> implements ICloseDebitAccount {
    readonly route: string = '/api/accounts/debit/{{accountId}}/close';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new CloseDebitAccountValidator();

    private _accountId!: string;

    constructor() {
        super(Object, false);
    }

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

    static use(initialValues?: ICloseDebitAccount): [CloseDebitAccount, SetCommandValues<ICloseDebitAccount>, ClearCommandValues] {
        return useCommand<CloseDebitAccount, ICloseDebitAccount>(CloseDebitAccount, initialValues);
    }
}
