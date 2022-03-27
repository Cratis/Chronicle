/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit');

export interface IOpenDebitAccount {
    accountId?: string;
    name?: string;
    owner?: string;
}

export class OpenDebitAccount extends Command implements IOpenDebitAccount {
    readonly route: string = '/api/accounts/debit';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;

    private _accountId!: string;
    private _name!: string;
    private _owner!: string;

    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
            'accountId',
            'name',
            'owner',
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

    static use(initialValues?: IOpenDebitAccount): [OpenDebitAccount, SetCommandValues<IOpenDebitAccount>] {
        return useCommand<OpenDebitAccount, IOpenDebitAccount>(OpenDebitAccount, initialValues);
    }
}
