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

export interface IReplayPartition {
    eventStore?: string;
    namespace?: string;
    observerId?: string;
    partition?: string;
}

export class ReplayPartitionValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        observerId: new Validator(),
        partition: new Validator(),
    };
}

export class ReplayPartition extends Command<IReplayPartition> implements IReplayPartition {
    readonly route: string = '/api/event-store/{eventStore}/observers/{namespace}/replay/{observerId}/{partition}';
    readonly validation: CommandValidator = new ReplayPartitionValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('eventStore', String),
        new PropertyDescriptor('namespace', String),
        new PropertyDescriptor('observerId', String),
        new PropertyDescriptor('partition', String),
    ];

    private _eventStore!: string;
    private _namespace!: string;
    private _observerId!: string;
    private _partition!: string;

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
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

    static use(initialValues?: IReplayPartition): [ReplayPartition, SetCommandValues<IReplayPartition>, ClearCommandValues] {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        return useCommand<ReplayPartition, IReplayPartition>(ReplayPartition, initialValues);
    }
}
