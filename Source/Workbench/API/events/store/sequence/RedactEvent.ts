/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import { Causation } from './Causation';
import { Identity } from './Identity';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/redact-event');

export interface IRedactEvent {
    microserviceId?: string;
    eventSequenceId?: string;
    tenantId?: string;
    sequenceNumber?: number;
    reason?: string;
    causation?: Causation;
    causedBy?: Identity;
}

export class RedactEventValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        eventSequenceId: new Validator(),
        tenantId: new Validator(),
        sequenceNumber: new Validator(),
        reason: new Validator(),
        causation: new Validator(),
        causedBy: new Validator(),
    };
}

export class RedactEvent extends Command<IRedactEvent> implements IRedactEvent {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/redact-event';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RedactEventValidator();

    private _microserviceId!: string;
    private _eventSequenceId!: string;
    private _tenantId!: string;
    private _sequenceNumber!: number;
    private _reason!: string;
    private _causation!: Causation;
    private _causedBy!: Identity;

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
            'sequenceNumber',
            'reason',
            'causation',
            'causedBy',
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
    get sequenceNumber(): number {
        return this._sequenceNumber;
    }

    set sequenceNumber(value: number) {
        this._sequenceNumber = value;
        this.propertyChanged('sequenceNumber');
    }
    get reason(): string {
        return this._reason;
    }

    set reason(value: string) {
        this._reason = value;
        this.propertyChanged('reason');
    }
    get causation(): Causation {
        return this._causation;
    }

    set causation(value: Causation) {
        this._causation = value;
        this.propertyChanged('causation');
    }
    get causedBy(): Identity {
        return this._causedBy;
    }

    set causedBy(value: Identity) {
        this._causedBy = value;
        this.propertyChanged('causedBy');
    }

    static use(initialValues?: IRedactEvent): [RedactEvent, SetCommandValues<IRedactEvent>, ClearCommandValues] {
        return useCommand<RedactEvent, IRedactEvent>(RedactEvent, initialValues);
    }
}
