/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/observers/{{observerId}}/rewind?microserviceId={{microserviceId}}&tenantId={{tenantId}}');

export interface IRewind {
    observerId?: string;
    microserviceId?: string;
    tenantId?: string;
}

export class RewindValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        observerId: new Validator(),
        microserviceId: new Validator(),
        tenantId: new Validator(),
    };
}

export class Rewind extends Command<IRewind> implements IRewind {
    readonly route: string = '/api/events/store/observers/{{observerId}}/rewind?microserviceId={{microserviceId}}&tenantId={{tenantId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RewindValidator();

    private _observerId!: string;
    private _microserviceId!: string;
    private _tenantId!: string;

    get requestArguments(): string[] {
        return [
            'observerId',
            'microserviceId',
            'tenantId',
        ];
    }

    get properties(): string[] {
        return [
            'observerId',
            'microserviceId',
            'tenantId',
        ];
    }

    get observerId(): string {
        return this._observerId;
    }

    set observerId(value: string) {
        this._observerId = value;
        this.propertyChanged('observerId');
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

    static use(initialValues?: IRewind): [Rewind, SetCommandValues<IRewind>] {
        return useCommand<Rewind, IRewind>(Rewind, initialValues);
    }
}
