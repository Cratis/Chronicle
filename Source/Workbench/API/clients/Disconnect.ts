/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/clients/{{microserviceId}}/disconnect/{{connectionId}}');

export interface IDisconnect {
    microserviceId?: string;
    connectionId?: string;
}

export class DisconnectValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        connectionId: new Validator(),
    };
}

export class Disconnect extends Command<IDisconnect> implements IDisconnect {
    readonly route: string = '/api/clients/{{microserviceId}}/disconnect/{{connectionId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new DisconnectValidator();

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

    static use(initialValues?: IDisconnect): [Disconnect, SetCommandValues<IDisconnect>, ClearCommandValues] {
        return useCommand<Disconnect, IDisconnect>(Disconnect, initialValues);
    }
}
