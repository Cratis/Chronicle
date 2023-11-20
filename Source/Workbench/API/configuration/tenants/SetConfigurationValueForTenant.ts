/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/configuration/tenants/{{tenantId}}');

export interface ISetConfigurationValueForTenant {
    tenantId?: string;
    key?: string;
    value?: string;
}

export class SetConfigurationValueForTenantValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        tenantId: new Validator(),
        key: new Validator(),
        value: new Validator(),
    };
}

export class SetConfigurationValueForTenant extends Command<ISetConfigurationValueForTenant> implements ISetConfigurationValueForTenant {
    readonly route: string = '/api/configuration/tenants/{{tenantId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new SetConfigurationValueForTenantValidator();

    private _tenantId!: string;
    private _key!: string;
    private _value!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'tenantId',
        ];
    }

    get properties(): string[] {
        return [
            'tenantId',
            'key',
            'value',
        ];
    }

    get tenantId(): string {
        return this._tenantId;
    }

    set tenantId(value: string) {
        this._tenantId = value;
        this.propertyChanged('tenantId');
    }
    get key(): string {
        return this._key;
    }

    set key(value: string) {
        this._key = value;
        this.propertyChanged('key');
    }
    get value(): string {
        return this._value;
    }

    set value(value: string) {
        this._value = value;
        this.propertyChanged('value');
    }

    static use(initialValues?: ISetConfigurationValueForTenant): [SetConfigurationValueForTenant, SetCommandValues<ISetConfigurationValueForTenant>, ClearCommandValues] {
        return useCommand<SetConfigurationValueForTenant, ISetConfigurationValueForTenant>(SetConfigurationValueForTenant, initialValues);
    }
}
