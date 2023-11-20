/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/observers/{{observerId}}/rewind/{{tenantId}}/{{eventSourceId}}');

export interface IRewindPartition {
    microserviceId?: string;
    tenantId?: string;
    observerId?: string;
    eventSourceId?: string;
}

export class RewindPartitionValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        tenantId: new Validator(),
        observerId: new Validator(),
        eventSourceId: new Validator(),
    };
}

export class RewindPartition extends Command<IRewindPartition> implements IRewindPartition {
    readonly route: string = '/api/events/store/{{microserviceId}}/observers/{{observerId}}/rewind/{{tenantId}}/{{eventSourceId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RewindPartitionValidator();

    private _microserviceId!: string;
    private _tenantId!: string;
    private _observerId!: string;
    private _eventSourceId!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'observerId',
            'eventSourceId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'observerId',
            'eventSourceId',
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
    get observerId(): string {
        return this._observerId;
    }

    set observerId(value: string) {
        this._observerId = value;
        this.propertyChanged('observerId');
    }
    get eventSourceId(): string {
        return this._eventSourceId;
    }

    set eventSourceId(value: string) {
        this._eventSourceId = value;
        this.propertyChanged('eventSourceId');
    }

    static use(initialValues?: IRewindPartition): [RewindPartition, SetCommandValues<IRewindPartition>, ClearCommandValues] {
        return useCommand<RewindPartition, IRewindPartition>(RewindPartition, initialValues);
    }
}
