/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/observers/{{observerId}}/replay/{{tenantId}}');

export interface IReplay {
    microserviceId?: string;
    tenantId?: string;
    observerId?: string;
}

export class ReplayValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        tenantId: new Validator(),
        observerId: new Validator(),
    };
}

export class Replay extends Command<IReplay> implements IReplay {
    readonly route: string = '/api/events/store/{{microserviceId}}/observers/{{observerId}}/replay/{{tenantId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new ReplayValidator();

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

    static use(initialValues?: IReplay): [Replay, SetCommandValues<IReplay>, ClearCommandValues] {
        return useCommand<Replay, IReplay>(Replay, initialValues);
    }
}
