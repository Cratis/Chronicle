/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import { ImmediateProjectionResult } from './ImmediateProjectionResult';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/projections/immediate/{{tenantId}}/session/{{correlationId}}');

export interface IImmediateForSession {
    microserviceId?: string;
    tenantId?: string;
    correlationId?: string;
    projectionId?: string;
    eventSequenceId?: string;
    modelKey?: string;
}

export class ImmediateForSessionValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        tenantId: new Validator(),
        correlationId: new Validator(),
        projectionId: new Validator(),
        eventSequenceId: new Validator(),
        modelKey: new Validator(),
    };
}

export class ImmediateForSession extends Command<IImmediateForSession, ImmediateProjectionResult> implements IImmediateForSession {
    readonly route: string = '/api/events/store/{{microserviceId}}/projections/immediate/{{tenantId}}/session/{{correlationId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new ImmediateForSessionValidator();

    private _microserviceId!: string;
    private _tenantId!: string;
    private _correlationId!: string;
    private _projectionId!: string;
    private _eventSequenceId!: string;
    private _modelKey!: string;

    constructor() {
        super(ImmediateProjectionResult, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'correlationId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'correlationId',
            'projectionId',
            'eventSequenceId',
            'modelKey',
        ];
    }

    get microserviceId(): string {
        return this._microserviceId;
    }

    set microserviceId(value: string) {
        this._microserviceId = value;
        this.propertyChanged('microserviceId');
    }
    get tenantId(): string {
        return this._tenantId;
    }

    set tenantId(value: string) {
        this._tenantId = value;
        this.propertyChanged('tenantId');
    }
    get correlationId(): string {
        return this._correlationId;
    }

    set correlationId(value: string) {
        this._correlationId = value;
        this.propertyChanged('correlationId');
    }
    get projectionId(): string {
        return this._projectionId;
    }

    set projectionId(value: string) {
        this._projectionId = value;
        this.propertyChanged('projectionId');
    }
    get eventSequenceId(): string {
        return this._eventSequenceId;
    }

    set eventSequenceId(value: string) {
        this._eventSequenceId = value;
        this.propertyChanged('eventSequenceId');
    }
    get modelKey(): string {
        return this._modelKey;
    }

    set modelKey(value: string) {
        this._modelKey = value;
        this.propertyChanged('modelKey');
    }

    static use(initialValues?: IImmediateForSession): [ImmediateForSession, SetCommandValues<IImmediateForSession>, ClearCommandValues] {
        return useCommand<ImmediateForSession, IImmediateForSession>(ImmediateForSession, initialValues);
    }
}
