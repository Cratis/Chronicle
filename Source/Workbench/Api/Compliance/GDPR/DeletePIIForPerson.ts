/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { Command, CommandValidator, CommandPropertyValidators } from '@cratis/applications/commands';
import { useCommand, SetCommandValues, ClearCommandValues } from '@cratis/applications.react/commands';
import { Validator } from '@cratis/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/pii/delete/{person}');

export interface IDeletePIIForPerson {
    person?: string;
}

export class DeletePIIForPersonValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        person: new Validator(),
    };
}

export class DeletePIIForPerson extends Command<IDeletePIIForPerson> implements IDeletePIIForPerson {
    readonly route: string = '/api/compliance/gdpr/pii/delete/{person}';
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
