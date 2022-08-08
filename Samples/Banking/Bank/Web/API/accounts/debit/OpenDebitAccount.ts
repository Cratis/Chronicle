/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit');

export interface IOpenDebitAccount {
    accountId?: string;
    name?: string;
    owner?: string;
    includeCard?: boolean;
}

export class OpenDebitAccountValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        accountId: new Validator(),
        name: new Validator(),
        owner: new Validator(),
        includeCard: new Validator(),
    };
}

export class OpenDebitAccount extends Command<IOpenDebitAccount> implements IOpenDebitAccount {
    readonly route: string = '/api/accounts/debit';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new OpenDebitAccountValidator();

    private _accountId!: string;
    private _name!: string;
    private _owner!: string;
    private _includeCard!: boolean;

    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
            'accountId',
            'name',
            'owner',
            'includeCard',
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
    get owner(): string {
        return this._owner;
    }

    set owner(value: string) {
        this._owner = value;
        this.propertyChanged('owner');
    }
    get includeCard(): boolean {
        return this._includeCard;
    }

    set includeCard(value: boolean) {
        this._includeCard = value;
        this.propertyChanged('includeCard');
    }

    static use(initialValues?: IOpenDebitAccount): [OpenDebitAccount, SetCommandValues<IOpenDebitAccount>] {
        return useCommand<OpenDebitAccount, IOpenDebitAccount>(OpenDebitAccount, initialValues);
    }
}
