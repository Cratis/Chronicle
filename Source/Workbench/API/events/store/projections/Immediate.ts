/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import { ImmediateProjectionResult } from './ImmediateProjectionResult';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/projections/immediate/{{tenantId}}');

export interface IImmediate {
    microserviceId?: string;
    tenantId?: string;
    projectionId?: string;
    eventSequenceId?: string;
    modelKey?: string;
    projection?: any;
}

export class ImmediateValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        tenantId: new Validator(),
        projectionId: new Validator(),
        eventSequenceId: new Validator(),
        modelKey: new Validator(),
        projection: new Validator(),
    };
}

export class Immediate extends Command<IImmediate, ImmediateProjectionResult> implements IImmediate {
    readonly route: string = '/api/events/store/{{microserviceId}}/projections/immediate/{{tenantId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new ImmediateValidator();

    private _microserviceId!: string;
    private _tenantId!: string;
    private _projectionId!: string;
    private _eventSequenceId!: string;
    private _modelKey!: string;
    private _projection!: any;

    constructor() {
        super(ImmediateProjectionResult, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'projectionId',
            'eventSequenceId',
            'modelKey',
            'projection',
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
    get projection(): any {
        return this._projection;
    }

    set projection(value: any) {
        this._projection = value;
        this.propertyChanged('projection');
    }

    static use(initialValues?: IImmediate): [Immediate, SetCommandValues<IImmediate>, ClearCommandValues] {
        return useCommand<Immediate, IImmediate>(Immediate, initialValues);
    }
}
