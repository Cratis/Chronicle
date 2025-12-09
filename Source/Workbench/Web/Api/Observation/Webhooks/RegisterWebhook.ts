/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
/* eslint-disable @typescript-eslint/no-empty-interface */
// eslint-disable-next-line header/header
import { Command, CommandPropertyValidators, CommandValidator } from '@cratis/arc/commands';
import { useCommand, SetCommandValues, ClearCommandValues } from '@cratis/arc.react/commands';
import { Validator } from '@cratis/arc/validation';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { EventType } from '../../Events/EventType';
import { WebhookTarget } from './WebhookTarget';
import { Guid } from '@cratis/fundamentals';

export interface IRegisterWebhook {
    eventStore?: string;
    eventSequenceId?: string;
    eventTypes?: EventType[];
    target?: WebhookTarget;
}

export class RegisterWebhookValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        eventSequenceId: new Validator(),
        eventTypes: new Validator(),
        target: new Validator(),
    };
}

export class RegisterWebhook extends Command<IRegisterWebhook, Guid> implements IRegisterWebhook {
    readonly route: string = '/api/event-store/{eventStore}/observers/webhooks/register';
    readonly validation: CommandValidator = new RegisterWebhookValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('eventStore', String),
        new PropertyDescriptor('eventSequenceId', String),
        new PropertyDescriptor('eventTypes', EventType),
        new PropertyDescriptor('target', WebhookTarget),
    ];

    private _eventStore!: string;
    private _eventSequenceId!: string;
    private _eventTypes!: EventType[];
    private _target!: WebhookTarget;

    constructor() {
        super(Guid, false);
    }

    get requestParameters(): string[] {
        return [
            'eventStore',
        ];
    }

    get properties(): string[] {
        return [
            'eventStore',
            'eventSequenceId',
            'eventTypes',
            'target',
        ];
    }

    get eventStore(): string {
        return this._eventStore;
    }

    set eventStore(value: string) {
        this._eventStore = value;
        this.propertyChanged('eventStore');
    }
    get eventSequenceId(): string {
        return this._eventSequenceId;
    }

    set eventSequenceId(value: string) {
        this._eventSequenceId = value;
        this.propertyChanged('eventSequenceId');
    }
    get eventTypes(): EventType[] {
        return this._eventTypes;
    }

    set eventTypes(value: EventType[]) {
        this._eventTypes = value;
        this.propertyChanged('eventTypes');
    }
    get target(): WebhookTarget {
        return this._target;
    }

    set target(value: WebhookTarget) {
        this._target = value;
        this.propertyChanged('target');
    }

    static use(initialValues?: IRegisterWebhook): [RegisterWebhook, SetCommandValues<IRegisterWebhook>, ClearCommandValues] {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        return useCommand<RegisterWebhook, IRegisterWebhook>(RegisterWebhook, initialValues);
    }
}
