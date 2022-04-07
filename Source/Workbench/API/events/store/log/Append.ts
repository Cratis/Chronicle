/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/log/{{eventSourceId}}/{{eventTypeId}}/{{eventGeneration}}');

export interface IAppend {
    eventSourceId?: string;
    eventTypeId?: string;
    eventGeneration?: number;
}

export class AppendValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventSourceId: new Validator(),
        eventTypeId: new Validator(),
        eventGeneration: new Validator(),
    };
}

export class Append extends Command<IAppend> implements IAppend {
    readonly route: string = '/api/events/store/log/{{eventSourceId}}/{{eventTypeId}}/{{eventGeneration}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AppendValidator();

    private _eventSourceId!: string;
    private _eventTypeId!: string;
    private _eventGeneration!: number;

    get requestArguments(): string[] {
        return [
            'eventSourceId',
            'eventTypeId',
            'eventGeneration',
        ];
    }

    get properties(): string[] {
        return [
            'eventSourceId',
            'eventTypeId',
            'eventGeneration',
        ];
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
