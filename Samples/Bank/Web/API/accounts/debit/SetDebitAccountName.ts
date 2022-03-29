/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/{{accountId}}/name/{{name}}');

export interface ISetDebitAccountName {
    accountId?: string;
    name?: string;
}

export class SetDebitAccountNameValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        accountId: new Validator(),
        name: new Validator(),
    };
}

export class SetDebitAccountName extends Command<ISetDebitAccountName> implements ISetDebitAccountName {
    readonly route: string = '/api/accounts/debit/{{accountId}}/name/{{name}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new SetDebitAccountNameValidator();

    private _accountId!: string;
    private _name!: string;

    get requestArguments(): string[] {
        return [
            'accountId',
            'name',
        ];
    }

    get properties(): string[] {
        return [
            'accountId',
            'name',
        ];
    }

    get accountId(): string {
        return this._accountId;
    }

    set accountId(value: string) {
        this._accountId = value;
        this.propertyChanged('accountId');
    }
    get name(): string {
        return this._name;
    }

    set name(value: string) {
        this._name = value;
        this.propertyChanged('name');
    }

    static use(initialValues?: ISetDebitAccountName): [SetDebitAccountName, SetCommandValues<ISetDebitAccountName>] {
        return useCommand<SetDebitAccountName, ISetDebitAccountName>(SetDebitAccountName, initialValues);
    }
}
