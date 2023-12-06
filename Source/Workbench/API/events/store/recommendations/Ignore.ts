/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/recommendations/{{recommendationId}}/ignore');

export interface IIgnore {
    microserviceId?: string;
    tenantId?: string;
    recommendationId?: string;
}

export class IgnoreValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        tenantId: new Validator(),
        recommendationId: new Validator(),
    };
}

export class Ignore extends Command<IIgnore> implements IIgnore {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/recommendations/{{recommendationId}}/ignore';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new IgnoreValidator();

    private _microserviceId!: string;
    private _tenantId!: string;
    private _recommendationId!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'recommendationId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'recommendationId',
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
    get recommendationId(): string {
        return this._recommendationId;
    }

    set recommendationId(value: string) {
        this._recommendationId = value;
        this.propertyChanged('recommendationId');
    }

    static use(initialValues?: IIgnore): [Ignore, SetCommandValues<IIgnore>, ClearCommandValues] {
        return useCommand<Ignore, IIgnore>(Ignore, initialValues);
    }
}
