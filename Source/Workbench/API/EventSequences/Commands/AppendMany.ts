/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import { AppendManyEvents } from './AppendManyEvents';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}');

export interface IAppendMany {
    eventStore?: string;
    namespace?: string;
    eventSequenceId?: string;
    eventsToAppend?: AppendManyEvents;
}

export class AppendManyValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        eventSequenceId: new Validator(),
        eventsToAppend: new Validator(),
    };
}

export class AppendMany extends Command<IAppendMany> implements IAppendMany {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AppendManyValidator();

    private _eventStore!: string;
    private _namespace!: string;
    private _eventSequenceId!: string;
    private _eventsToAppend!: AppendManyEvents;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'eventSequenceId',
        ];
    }

    get properties(): string[] {
        return [
            'eventStore',
            'namespace',
            'eventSequenceId',
            'eventsToAppend',
        ];
    }

    get eventStore(): string {
        return this._eventStore;
    }

    set eventStore(value: string) {
        this._eventStore = value;
        this.propertyChanged('eventStore');
    }
    get namespace(): string {
        return this._namespace;
    }

    set namespace(value: string) {
        this._namespace = value;
        this.propertyChanged('namespace');
    }
    get eventSequenceId(): string {
        return this._eventSequenceId;
    }

    set eventSequenceId(value: string) {
        this._eventSequenceId = value;
        this.propertyChanged('eventSequenceId');
    }
    get eventsToAppend(): AppendManyEvents {
        return this._eventsToAppend;
    }

    set eventsToAppend(value: AppendManyEvents) {
        this._eventsToAppend = value;
        this.propertyChanged('eventsToAppend');
    }

    static use(initialValues?: IAppendMany): [AppendMany, SetCommandValues<IAppendMany>, ClearCommandValues] {
        return useCommand<AppendMany, IAppendMany>(AppendMany, initialValues);
    }
}
