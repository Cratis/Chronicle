/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import { EventType } from '../sequence/EventType';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}');

export interface IAppend {
    microserviceId?: string;
    eventSequenceId?: string;
    tenantId?: string;
    eventSourceId?: string;
    eventType?: EventType;
    content?: any;
}

export class AppendValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        eventSequenceId: new Validator(),
        tenantId: new Validator(),
        eventSourceId: new Validator(),
        eventType: new Validator(),
        content: new Validator(),
    };
}

export class Append extends Command<IAppend> implements IAppend {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AppendValidator();

    private _microserviceId!: string;
    private _eventSequenceId!: string;
    private _tenantId!: string;
    private _eventSourceId!: string;
    private _eventType!: EventType;
    private _content!: any;

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
            'eventType',
            'content',
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
    get eventType(): EventType {
        return this._eventType;
    }

    set eventType(value: EventType) {
        this._eventType = value;
        this.propertyChanged('eventType');
    }
    get content(): any {
        return this._content;
    }

    set content(value: any) {
        this._content = value;
        this.propertyChanged('content');
    }

    static use(initialValues?: IAppend): [Append, SetCommandValues<IAppend>, ClearCommandValues] {
        return useCommand<Append, IAppend>(Append, initialValues);
    }
}
