/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from 'Infrastructure/commands';
import { Validator } from 'Infrastructure/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/jobs');

export interface IStopJob {
    eventStore?: string;
    namespace?: string;
    jobId?: string;
}

export class StopJobValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        jobId: new Validator(),
    };
}

export class StopJob extends Command<IStopJob> implements IStopJob {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/jobs';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new StopJobValidator();

    private _eventStore!: string;
    private _namespace!: string;
    private _jobId!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
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
    get jobId(): string {
        return this._jobId;
    }

    set jobId(value: string) {
        this._jobId = value;
        this.propertyChanged('jobId');
    }

    static use(initialValues?: IStopJob): [StopJob, SetCommandValues<IStopJob>, ClearCommandValues] {
        return useCommand<StopJob, IStopJob>(StopJob, initialValues);
    }
}
