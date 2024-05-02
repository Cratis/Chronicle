// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from 'Infrastructure/commands';
import { Validator } from 'Infrastructure/validation';
import { RedactEvent } from './RedactEvent';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}');

export interface IRedact {
    eventStore?: string;
    namespace?: string;
    eventSequenceId?: string;
    redaction?: RedactEvent;
}

export class RedactValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        eventSequenceId: new Validator(),
        redaction: new Validator(),
    };
}

export class Redact extends Command<IRedact> implements IRedact {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RedactValidator();

    private _eventStore!: string;
    private _namespace!: string;
    private _eventSequenceId!: string;
    private _redaction!: RedactEvent;

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
            'redaction',
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
    get redaction(): RedactEvent {
        return this._redaction;
    }

    set redaction(value: RedactEvent) {
        this._redaction = value;
        this.propertyChanged('redaction');
    }

    static use(initialValues?: IRedact): [Redact, SetCommandValues<IRedact>, ClearCommandValues] {
        return useCommand<Redact, IRedact>(Redact, initialValues);
    }
}
