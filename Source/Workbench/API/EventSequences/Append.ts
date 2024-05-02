// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from 'Infrastructure/commands';
import { Validator } from 'Infrastructure/validation';
import { AppendEvent } from './AppendEvent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}');

export interface IAppend {
    eventStore?: string;
    namespace?: string;
    eventSequenceId?: string;
    eventToAppend?: AppendEvent;
}

export class AppendValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        eventSequenceId: new Validator(),
        eventToAppend: new Validator(),
    };
}

export class Append extends Command<IAppend> implements IAppend {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AppendValidator();

    private _eventStore!: string;
    private _namespace!: string;
    private _eventSequenceId!: string;
    private _eventToAppend!: AppendEvent;

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
            'eventToAppend',
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
    get eventToAppend(): AppendEvent {
        return this._eventToAppend;
    }

    set eventToAppend(value: AppendEvent) {
        this._eventToAppend = value;
        this.propertyChanged('eventToAppend');
    }

    static use(initialValues?: IAppend): [Append, SetCommandValues<IAppend>, ClearCommandValues] {
        return useCommand<Append, IAppend>(Append, initialValues);
    }
}
