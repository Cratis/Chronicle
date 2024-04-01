/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/pii');

export interface IDeletePIIForPerson {
    person?: string;
}

export class DeletePIIForPersonValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        person: new Validator(),
    };
}

export class DeletePIIForPerson extends Command<IDeletePIIForPerson> implements IDeletePIIForPerson {
    readonly route: string = '/api/compliance/gdpr/pii';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new DeletePIIForPersonValidator();

    private _person!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'person',
        ];
    }

    get properties(): string[] {
        return [
            'person',
        ];
    }

    get person(): string {
        return this._person;
    }

    set person(value: string) {
        this._person = value;
        this.propertyChanged('person');
    }

    static use(initialValues?: IDeletePIIForPerson): [DeletePIIForPerson, SetCommandValues<IDeletePIIForPerson>, ClearCommandValues] {
        return useCommand<DeletePIIForPerson, IDeletePIIForPerson>(DeletePIIForPerson, initialValues);
    }
}
