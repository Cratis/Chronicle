/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import { AccountDetails } from './AccountDetails';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit');

export interface IOpenDebitAccount {
    accountId?: string;
    details?: AccountDetails;
}

export class OpenDebitAccountValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        accountId: new Validator(),
        details: new Validator(),
    };
}

export class OpenDebitAccount extends Command<IOpenDebitAccount> implements IOpenDebitAccount {
    readonly route: string = '/api/accounts/debit';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new OpenDebitAccountValidator();

    private _accountId!: string;
    private _details!: AccountDetails;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
            'accountId',
            'details',
        ];
    }

    get accountId(): string {
        return this._accountId;
    }

    set accountId(value: string) {
        this._accountId = value;
        this.propertyChanged('accountId');
    }
    get details(): AccountDetails {
        return this._details;
    }

    set details(value: AccountDetails) {
        this._details = value;
        this.propertyChanged('details');
    }

    static use(initialValues?: IOpenDebitAccount): [OpenDebitAccount, SetCommandValues<IOpenDebitAccount>, ClearCommandValues] {
        return useCommand<OpenDebitAccount, IOpenDebitAccount>(OpenDebitAccount, initialValues);
    }
}
