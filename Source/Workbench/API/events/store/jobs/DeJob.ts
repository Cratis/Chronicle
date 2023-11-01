/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/jobs/{{jobId}}/stop/');

export interface IDeJob {
    microserviceId?: string;
    tenantId?: string;
    jobId?: string;
}

export class DeJobValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        tenantId: new Validator(),
        jobId: new Validator(),
    };
}

export class DeJob extends Command<IDeJob> implements IDeJob {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/jobs/{{jobId}}/stop/';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new DeJobValidator();

    private _microserviceId!: string;
    private _tenantId!: string;
    private _jobId!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'jobId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'jobId',
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
    get jobId(): string {
        return this._jobId;
    }

    set jobId(value: string) {
        this._jobId = value;
        this.propertyChanged('jobId');
    }

    static use(initialValues?: IDeJob): [DeJob, SetCommandValues<IDeJob>, ClearCommandValues] {
        return useCommand<DeJob, IDeJob>(DeJob, initialValues);
    }
}
