/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import { EventToApply } from './EventToApply';
import { ImmediateProjectionResult } from './ImmediateProjectionResult';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/projections/immediate/{{tenantId}}/session/{{correlationId}}/with-events');

export interface IImmediateForSessionWithEvents {
    microserviceId?: string;
    tenantId?: string;
    correlationId?: string;
    projectionId?: string;
    eventSequenceId?: string;
    modelKey?: string;
    events?: EventToApply[];
}

export class ImmediateForSessionWithEventsValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        tenantId: new Validator(),
        correlationId: new Validator(),
        projectionId: new Validator(),
        eventSequenceId: new Validator(),
        modelKey: new Validator(),
        events: new Validator(),
    };
}

export class ImmediateForSessionWithEvents extends Command<IImmediateForSessionWithEvents, ImmediateProjectionResult> implements IImmediateForSessionWithEvents {
    readonly route: string = '/api/events/store/{{microserviceId}}/projections/immediate/{{tenantId}}/session/{{correlationId}}/with-events';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new ImmediateForSessionWithEventsValidator();

    private _microserviceId!: string;
    private _tenantId!: string;
    private _correlationId!: string;
    private _projectionId!: string;
    private _eventSequenceId!: string;
    private _modelKey!: string;
    private _events!: EventToApply[];

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
            'events',
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
    get events(): EventToApply[] {
        return this._events;
    }

    set events(value: EventToApply[]) {
        this._events = value;
        this.propertyChanged('events');
    }

    static use(initialValues?: IImmediateForSessionWithEvents): [ImmediateForSessionWithEvents, SetCommandValues<IImmediateForSessionWithEvents>, ClearCommandValues] {
        return useCommand<ImmediateForSessionWithEvents, IImmediateForSessionWithEvents>(ImmediateForSessionWithEvents, initialValues);
    }
}
