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
import { EventType } from '../Events/EventType';
import { Identity } from '../Identities/Identity';

export interface IAppend {
    eventStore?: string;
    namespace?: string;
    eventSequenceId?: string;
    eventSourceId?: string;
    eventStreamType?: string;
    eventStreamId?: string;
    eventType?: EventType;
    content?: any;
    causation?: Causation[];
    causedBy?: Identity;
}

export class AppendValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        eventSequenceId: new Validator(),
        eventSourceId: new Validator(),
        eventStreamType: new Validator(),
        eventStreamId: new Validator(),
        eventType: new Validator(),
        content: new Validator(),
        causation: new Validator(),
        causedBy: new Validator(),
    };
}

export class Append extends Command<IAppend> implements IAppend {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}';
    readonly validation: CommandValidator = new AppendValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('eventStore', String),
        new PropertyDescriptor('namespace', String),
        new PropertyDescriptor('eventSequenceId', String),
        new PropertyDescriptor('eventSourceId', String),
        new PropertyDescriptor('eventStreamType', String),
        new PropertyDescriptor('eventStreamId', String),
        new PropertyDescriptor('eventType', EventType),
        new PropertyDescriptor('content', Object),
        new PropertyDescriptor('causation', Causation),
        new PropertyDescriptor('causedBy', Identity),
    ];

    private _eventStore!: string;
    private _namespace!: string;
    private _eventSequenceId!: string;
    private _eventSourceId!: string;
    private _eventStreamType!: string;
    private _eventStreamId!: string;
    private _eventType!: EventType;
    private _content!: any;
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
            'eventSourceId',
            'eventStreamType',
            'eventStreamId',
            'eventType',
            'content',
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
    get eventSourceId(): string {
        return this._eventSourceId;
    }

    set eventSourceId(value: string) {
        this._eventSourceId = value;
        this.propertyChanged('eventSourceId');
    }
    get eventStreamType(): string {
        return this._eventStreamType;
    }

    set eventStreamType(value: string) {
        this._eventStreamType = value;
        this.propertyChanged('eventStreamType');
    }
    get eventStreamId(): string {
        return this._eventStreamId;
    }

    set eventStreamId(value: string) {
        this._eventStreamId = value;
        this.propertyChanged('eventStreamId');
    }
    get eventType(): EventType {
        return this._eventType;
    }

    set eventType(value: EventType) {
        this._eventType = value;
        this.propertyChanged('eventType');
    }
    get content(): any {
        return this._content;
    }

    set content(value: any) {
        this._content = value;
        this.propertyChanged('content');
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

    static use(initialValues?: IAppend): [Append, SetCommandValues<IAppend>, ClearCommandValues] {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        return useCommand<Append, IAppend>(Append, initialValues);
    }
}
