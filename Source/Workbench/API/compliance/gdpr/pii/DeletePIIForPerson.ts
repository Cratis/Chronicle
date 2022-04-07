/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/pii/delete');

export interface IDeletePIIForPerson {
    personId?: string;
}

export class DeletePIIForPersonValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        personId: new Validator(),
    };
}

export class DeletePIIForPerson extends Command<IDeletePIIForPerson> implements IDeletePIIForPerson {
    readonly route: string = '/api/compliance/gdpr/pii/delete';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new DeletePIIForPersonValidator();

    private _personId!: string;

    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
            'personId',
        ];
    }

    get personId(): string {
        return this._personId;
    }

    set personId(value: string) {
        this._personId = value;
        this.propertyChanged('personId');
    }

    static use(initialValues?: IDeletePIIForPerson): [DeletePIIForPerson, SetCommandValues<IDeletePIIForPerson>] {
        return useCommand<DeletePIIForPerson, IDeletePIIForPerson>(DeletePIIForPerson, initialValues);
    }
}
