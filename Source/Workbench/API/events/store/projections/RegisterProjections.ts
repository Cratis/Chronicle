/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import { ProjectionRegistration } from './ProjectionRegistration';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/projections');

export interface IRegisterProjections {
    microserviceId?: string;
    projections?: ProjectionRegistration[];
}

export class RegisterProjectionsValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        projections: new Validator(),
    };
}

export class RegisterProjections extends Command<IRegisterProjections> implements IRegisterProjections {
    readonly route: string = '/api/events/store/{{microserviceId}}/projections';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RegisterProjectionsValidator();

    private _microserviceId!: string;
    private _projections!: ProjectionRegistration[];

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'projections',
        ];
    }

    get microserviceId(): string {
        return this._microserviceId;
    }

    set microserviceId(value: string) {
        this._microserviceId = value;
        this.propertyChanged('microserviceId');
    }
    get projections(): ProjectionRegistration[] {
        return this._projections;
    }

    set projections(value: ProjectionRegistration[]) {
        this._projections = value;
        this.propertyChanged('projections');
    }

    static use(initialValues?: IRegisterProjections): [RegisterProjections, SetCommandValues<IRegisterProjections>, ClearCommandValues] {
        return useCommand<RegisterProjections, IRegisterProjections>(RegisterProjections, initialValues);
    }
}
