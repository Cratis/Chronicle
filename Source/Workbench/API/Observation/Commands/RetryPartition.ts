/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/observers');

export interface IRetryPartition {
    eventStore?: string;
    namespace?: string;
    observerId?: string;
    partition?: string;
}

export class RetryPartitionValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        observerId: new Validator(),
        partition: new Validator(),
    };
}

export class RetryPartition extends Command<IRetryPartition> implements IRetryPartition {
    readonly route: string = '/api/events/store/{eventStore}/observers';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RetryPartitionValidator();

    private _eventStore!: string;
    private _namespace!: string;
    private _observerId!: string;
    private _partition!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'observerId',
            'partition',
        ];
    }

    get properties(): string[] {
        return [
            'eventStore',
            'namespace',
            'observerId',
            'partition',
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
    get observerId(): string {
        return this._observerId;
    }

    set observerId(value: string) {
        this._observerId = value;
        this.propertyChanged('observerId');
    }
    get partition(): string {
        return this._partition;
    }

    set partition(value: string) {
        this._partition = value;
        this.propertyChanged('partition');
    }

    static use(initialValues?: IRetryPartition): [RetryPartition, SetCommandValues<IRetryPartition>, ClearCommandValues] {
        return useCommand<RetryPartition, IRetryPartition>(RetryPartition, initialValues);
    }
}
