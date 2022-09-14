/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
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

    static use(initialValues?: IOpenDebitAccount): [OpenDebitAccount, SetCommandValues<IOpenDebitAccount>] {
        return useCommand<OpenDebitAccount, IOpenDebitAccount>(OpenDebitAccount, initialValues);
    }
}
