/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import { AccountDetails } from './AccountDetails';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/credit');

export interface IOpenCreditAccount {
    accountId?: string;
    details?: AccountDetails;
}

export class OpenCreditAccountValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        accountId: new Validator(),
        details: new Validator(),
    };
}

export class OpenCreditAccount extends Command<IOpenCreditAccount> implements IOpenCreditAccount {
    readonly route: string = '/api/accounts/credit';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new OpenCreditAccountValidator();

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

    static use(initialValues?: IOpenCreditAccount): [OpenCreditAccount, SetCommandValues<IOpenCreditAccount>] {
        return useCommand<OpenCreditAccount, IOpenCreditAccount>(OpenCreditAccount, initialValues);
    }
}
