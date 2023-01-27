/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/clients/{{microserviceId}}/connect/{{connectionId}}?insideKernel={{insideKernel}}');

export interface IConnect {
    microserviceId?: string;
    connectionId?: string;
    clientVersion?: string;
    advertisedUri?: string;
    insideKernel?: boolean;
}

export class ConnectValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        connectionId: new Validator(),
        clientVersion: new Validator(),
        advertisedUri: new Validator(),
        insideKernel: new Validator(),
    };
}

export class Connect extends Command<IConnect> implements IConnect {
    readonly route: string = '/api/clients/{{microserviceId}}/connect/{{connectionId}}?insideKernel={{insideKernel}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new ConnectValidator();

    private _microserviceId!: string;
    private _connectionId!: string;
    private _clientVersion!: string;
    private _advertisedUri!: string;
    private _insideKernel!: boolean;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'connectionId',
            'insideKernel',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'connectionId',
            'clientVersion',
            'advertisedUri',
            'insideKernel',
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
    get clientVersion(): string {
        return this._clientVersion;
    }

    set clientVersion(value: string) {
        this._clientVersion = value;
        this.propertyChanged('clientVersion');
    }
    get advertisedUri(): string {
        return this._advertisedUri;
    }

    set advertisedUri(value: string) {
        this._advertisedUri = value;
        this.propertyChanged('advertisedUri');
    }
    get insideKernel(): boolean {
        return this._insideKernel;
    }

    set insideKernel(value: boolean) {
        this._insideKernel = value;
        this.propertyChanged('insideKernel');
    }

    static use(initialValues?: IConnect): [Connect, SetCommandValues<IConnect>, ClearCommandValues] {
        return useCommand<Connect, IConnect>(Connect, initialValues);
    }
}
