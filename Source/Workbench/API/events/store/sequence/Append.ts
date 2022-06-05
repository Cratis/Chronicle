/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/sequence/{{eventSequenceId}}/{{microserviceId}}/{{tenantId}}/{{eventSourceId}}/{{eventTypeId}}/{{eventGeneration}}');

export interface IAppend {
    eventSequenceId?: string;
    microserviceId?: string;
    tenantId?: string;
    eventSourceId?: string;
    eventTypeId?: string;
    eventGeneration?: number;
}

export class AppendValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventSequenceId: new Validator(),
        microserviceId: new Validator(),
        tenantId: new Validator(),
        eventSourceId: new Validator(),
        eventTypeId: new Validator(),
        eventGeneration: new Validator(),
    };
}

export class Append extends Command<IAppend> implements IAppend {
    readonly route: string = '/api/events/store/sequence/{{eventSequenceId}}/{{microserviceId}}/{{tenantId}}/{{eventSourceId}}/{{eventTypeId}}/{{eventGeneration}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AppendValidator();

    private _eventSequenceId!: string;
    private _microserviceId!: string;
    private _tenantId!: string;
    private _eventSourceId!: string;
    private _eventTypeId!: string;
    private _eventGeneration!: number;

    get requestArguments(): string[] {
        return [
            'eventSequenceId',
            'microserviceId',
            'tenantId',
            'eventSourceId',
            'eventTypeId',
            'eventGeneration',
        ];
    }

    get properties(): string[] {
        return [
            'eventSequenceId',
            'microserviceId',
            'tenantId',
            'eventSourceId',
            'eventTypeId',
            'eventGeneration',
        ];
    }

    get eventSequenceId(): string {
        return this._eventSequenceId;
    }

    set eventSequenceId(value: string) {
        this._eventSequenceId = value;
        this.propertyChanged('eventSequenceId');
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
    get eventSourceId(): string {
        return this._eventSourceId;
    }

    set eventSourceId(value: string) {
        this._eventSourceId = value;
        this.propertyChanged('eventSourceId');
    }
    get eventTypeId(): string {
        return this._eventTypeId;
    }

    set eventTypeId(value: string) {
        this._eventTypeId = value;
        this.propertyChanged('eventTypeId');
    }
    get eventGeneration(): number {
        return this._eventGeneration;
    }

    set eventGeneration(value: number) {
        this._eventGeneration = value;
        this.propertyChanged('eventGeneration');
    }

    static use(initialValues?: IAppend): [Append, SetCommandValues<IAppend>] {
        return useCommand<Append, IAppend>(Append, initialValues);
    }
}
