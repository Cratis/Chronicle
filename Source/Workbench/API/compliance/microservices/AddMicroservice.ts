/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/microservices');

export interface IAddMicroservice {
    microserviceId?: string;
    name?: string;
}

export class AddMicroserviceValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        name: new Validator(),
    };
}

export class AddMicroservice extends Command<IAddMicroservice> implements IAddMicroservice {
    readonly route: string = '/api/compliance/microservices';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AddMicroserviceValidator();

    private _microserviceId!: string;
    private _name!: string;

    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'name',
        ];
    }

    get microserviceId(): string {
        return this._microserviceId;
    }

    set microserviceId(value: string) {
        this._microserviceId = value;
        this.propertyChanged('microserviceId');
    }
    get name(): string {
        return this._name;
    }

    set name(value: string) {
        this._name = value;
        this.propertyChanged('name');
    }

    static use(initialValues?: IAddMicroservice): [AddMicroservice, SetCommandValues<IAddMicroservice>] {
        return useCommand<AddMicroservice, IAddMicroservice>(AddMicroservice, initialValues);
    }
}
