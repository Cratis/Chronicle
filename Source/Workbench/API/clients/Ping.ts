/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/clients/{{microserviceId}}/ping/{{connectionId}}');

export interface IPing {
    microserviceId?: string;
    connectionId?: string;
}

export class PingValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        connectionId: new Validator(),
    };
}

export class Ping extends Command<IPing> implements IPing {
    readonly route: string = '/api/clients/{{microserviceId}}/ping/{{connectionId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new PingValidator();

    private _microserviceId!: string;
    private _connectionId!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'connectionId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'connectionId',
        ];
    }

    get microserviceId(): string {
        return this._microserviceId;
    }

    set microserviceId(value: string) {
        this._microserviceId = value;
        this.propertyChanged('microserviceId');
    }
    get connectionId(): string {
        return this._connectionId;
    }

    set connectionId(value: string) {
        this._connectionId = value;
        this.propertyChanged('connectionId');
    }

    static use(initialValues?: IPing): [Ping, SetCommandValues<IPing>, ClearCommandValues] {
        return useCommand<Ping, IPing>(Ping, initialValues);
    }
}
