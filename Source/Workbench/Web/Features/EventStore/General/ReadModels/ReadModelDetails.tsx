// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { ReadModelDefinition } from 'Api/ReadModels';
import { SchemaEditor } from 'Components/SchemaEditor/SchemaEditor';
import strings from 'Strings';

export interface ReadModelDetailsProps {
    item: ReadModelDefinition;
}

export const ReadModelDetails = ({ item }: ReadModelDetailsProps) => {
    const [readModel] = useState<ReadModelDefinition>(item);

    return (
        <div className="read-model-details">
            <h3>{readModel.name}</h3>
            <div className="generation-info">
                {strings.eventStore.general.readModels.columns.generation}: {readModel.generation}
            </div>
            <SchemaEditor 
                schema={readModel.schema}
                onSave={(schema) => {
                    // TODO: Call update API
                    console.log('Schema saved:', schema);
                }}
            />
        </div>
    );
};
