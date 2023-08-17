/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import { EventToAppend } from '../sequence/EventToAppend';
import { Causation } from '../sequence/Causation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/append-many');

export interface IAppendMany {
    microserviceId?: string;
    eventSequenceId?: string;
    tenantId?: string;
    eventSourceId?: string;
    events?: EventToAppend[];
    causation?: Causation[];
}

export class AppendManyValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        eventSequenceId: new Validator(),
        tenantId: new Validator(),
        eventSourceId: new Validator(),
        events: new Validator(),
        causation: new Validator(),
    };
}

export class AppendMany extends Command<IAppendMany> implements IAppendMany {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/append-many';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AppendManyValidator();

    private _microserviceId!: string;
    private _eventSequenceId!: string;
    private _tenantId!: string;
    private _eventSourceId!: string;
    private _events!: EventToAppend[];
    private _causation!: Causation[];

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'eventSequenceId',
            'tenantId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'eventSequenceId',
            'tenantId',
            'eventSourceId',
            'events',
            'causation',
        ];
    }

    get microserviceId(): string {
        return this._microserviceId;
    }

    set microserviceId(value: string) {
        this._microserviceId = value;
        this.propertyChanged('microserviceId');
    }
    get eventSequenceId(): string {
        return this._eventSequenceId;
    }

    set eventSequenceId(value: string) {
        this._eventSequenceId = value;
        this.propertyChanged('eventSequenceId');
    }
    get tenantId(): string {
        return this._tenantId;
    }

    set tenantId(value: string) {
        this._tenantId = value;
        this.propertyChanged('tenantId');
    }
    get eventSourceId(): string {
        return this._eventSourceId;
    }

    set eventSourceId(value: string) {
        this._eventSourceId = value;
        this.propertyChanged('eventSourceId');
    }
    get events(): EventToAppend[] {
        return this._events;
    }

    set events(value: EventToAppend[]) {
        this._events = value;
        this.propertyChanged('events');
    }
    get causation(): Causation[] {
        return this._causation;
    }

    set causation(value: Causation[]) {
        this._causation = value;
        this.propertyChanged('causation');
    }

    static use(initialValues?: IAppendMany): [AppendMany, SetCommandValues<IAppendMany>, ClearCommandValues] {
        return useCommand<AppendMany, IAppendMany>(AppendMany, initialValues);
    }
}
