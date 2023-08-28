/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/clients/{{microserviceId}}/connect/{{connectionId}}');

export interface IConnect {
    microserviceId?: string;
    connectionId?: string;
    clientVersion?: string;
    advertisedUri?: string;
    isRunningWithDebugger?: boolean;
    isMultiTenanted?: boolean;
}

export class ConnectValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        connectionId: new Validator(),
        clientVersion: new Validator(),
        advertisedUri: new Validator(),
        isRunningWithDebugger: new Validator(),
        isMultiTenanted: new Validator(),
    };
}

export class Connect extends Command<IConnect> implements IConnect {
    readonly route: string = '/api/clients/{{microserviceId}}/connect/{{connectionId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new ConnectValidator();

    private _microserviceId!: string;
    private _connectionId!: string;
    private _clientVersion!: string;
    private _advertisedUri!: string;
    private _isRunningWithDebugger!: boolean;
    private _isMultiTenanted!: boolean;

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
            'clientVersion',
            'advertisedUri',
            'isRunningWithDebugger',
            'isMultiTenanted',
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
    get isRunningWithDebugger(): boolean {
        return this._isRunningWithDebugger;
    }

    set isRunningWithDebugger(value: boolean) {
        this._isRunningWithDebugger = value;
        this.propertyChanged('isRunningWithDebugger');
    }
    get isMultiTenanted(): boolean {
        return this._isMultiTenanted;
    }

    set isMultiTenanted(value: boolean) {
        this._isMultiTenanted = value;
        this.propertyChanged('isMultiTenanted');
    }

    static use(initialValues?: IConnect): [Connect, SetCommandValues<IConnect>, ClearCommandValues] {
        return useCommand<Connect, IConnect>(Connect, initialValues);
    }
}
