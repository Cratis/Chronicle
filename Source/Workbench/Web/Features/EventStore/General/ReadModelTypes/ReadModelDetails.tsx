// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';
import { IDetailsComponentProps } from 'Components';
import { SchemaEditor, JSONSchemaType } from 'Components';
import { ReadModelDefinition } from 'Api/ReadModelTypes';

export const ReadModelDetails = (props: IDetailsComponentProps<ReadModelDefinition>) => {
    const [schema, setSchema] = useState<JSONSchemaType>(() => JSON.parse(props.item.schema));

    // Reset schema when read model changes
    useEffect(() => {
        setSchema(JSON.parse(props.item.schema));
    }, [props.item.identifier, props.item.schema]);

    const handleSave = async () => {
        // TODO: Once the UpdateReadModelDefinition command is generated, use it here
        // For now, log the save action
        console.log('Saving read model definition:', {
            identifier: props.item.identifier,
            name: props.item.name,
            generation: props.item.generation,
            schema: JSON.stringify(schema, null, 2),
            indexes: props.item.indexes
        });

        // This will be replaced with:
        // updateReadModelDefinition.eventStore = params.eventStore!;
        // updateReadModelDefinition.identifier = props.item.identifier;
        // updateReadModelDefinition.name = props.item.name;
        // updateReadModelDefinition.generation = props.item.generation;
        // updateReadModelDefinition.schema = JSON.stringify(schema, null, 2);
        // updateReadModelDefinition.indexes = Array.from(props.item.indexes);
        // await updateReadModelDefinition.execute();
    };

    const handleSchemaChange = (newSchema: JSONSchemaType) => {
        setSchema(newSchema);
    };

    return (
        <div className="read-model-details" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <SchemaEditor
                schema={schema}
                eventTypeName={props.item.name}
                canEdit={true}
                onChange={handleSchemaChange}
                onSave={handleSave}
            />
        </div>
    );
};
