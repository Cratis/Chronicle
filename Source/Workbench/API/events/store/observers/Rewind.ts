/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/observers/{{observerId}}/rewind');

export interface IRewind {
    microserviceId?: string;
    tenantId?: string;
    observerId?: string;
}

export class RewindValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        tenantId: new Validator(),
        observerId: new Validator(),
    };
}

export class Rewind extends Command<IRewind> implements IRewind {
    readonly route: string = '/api/events/store/{{microserviceId}}/observers/{{observerId}}/rewind';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RewindValidator();

    private _microserviceId!: string;
    private _tenantId!: string;
    private _observerId!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'observerId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'observerId',
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

    static use(initialValues?: IRewind): [Rewind, SetCommandValues<IRewind>, ClearCommandValues] {
        return useCommand<Rewind, IRewind>(Rewind, initialValues);
    }
}
