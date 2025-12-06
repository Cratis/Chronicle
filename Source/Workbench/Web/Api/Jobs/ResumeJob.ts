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
import { Guid } from '@cratis/fundamentals';

export interface IResumeJob {
    eventStore?: string;
    namespace?: string;
    jobId?: Guid;
}

export class ResumeJobValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        jobId: new Validator(),
    };
}

export class ResumeJob extends Command<IResumeJob> implements IResumeJob {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/jobs/{jobId}/resume';
    readonly validation: CommandValidator = new ResumeJobValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('eventStore', String),
        new PropertyDescriptor('namespace', String),
        new PropertyDescriptor('jobId', Guid),
    ];

    private _eventStore!: string;
    private _namespace!: string;
    private _jobId!: Guid;

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [
            'eventStore',
            'namespace',
            'jobId',
        ];
    }

    get properties(): string[] {
        return [
            'eventStore',
            'namespace',
            'jobId',
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
    get jobId(): Guid {
        return this._jobId;
    }

    set jobId(value: Guid) {
        this._jobId = value;
        this.propertyChanged('jobId');
    }

    static use(initialValues?: IResumeJob): [ResumeJob, SetCommandValues<IResumeJob>, ClearCommandValues] {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        return useCommand<ResumeJob, IResumeJob>(ResumeJob, initialValues);
    }
}
