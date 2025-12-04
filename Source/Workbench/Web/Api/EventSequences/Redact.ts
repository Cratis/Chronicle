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
import { Causation } from '../Auditing/Causation';
import { Identity } from '../Identities/Identity';

export interface IRedact {
    eventStore?: string;
    namespace?: string;
    eventSequenceId?: string;
    sequenceNumber?: number;
    reason?: string;
    causation?: Causation[];
    causedBy?: Identity;
}

export class RedactValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        eventSequenceId: new Validator(),
        sequenceNumber: new Validator(),
        reason: new Validator(),
        causation: new Validator(),
        causedBy: new Validator(),
    };
}

export class Redact extends Command<IRedact> implements IRedact {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}/redact-event';
    readonly validation: CommandValidator = new RedactValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('eventStore', String),
        new PropertyDescriptor('namespace', String),
        new PropertyDescriptor('eventSequenceId', String),
        new PropertyDescriptor('sequenceNumber', Number),
        new PropertyDescriptor('reason', String),
        new PropertyDescriptor('causation', Causation),
        new PropertyDescriptor('causedBy', Identity),
    ];

    private _eventStore!: string;
    private _namespace!: string;
    private _eventSequenceId!: string;
    private _sequenceNumber!: number;
    private _reason!: string;
    private _causation!: Causation[];
    private _causedBy!: Identity;

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
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
            'sequenceNumber',
            'reason',
            'causation',
            'causedBy',
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
    get causation(): Causation[] {
        return this._causation;
    }

    set causation(value: Causation[]) {
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

    static use(initialValues?: IRedact): [Redact, SetCommandValues<IRedact>, ClearCommandValues] {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        return useCommand<Redact, IRedact>(Redact, initialValues);
    }
}
