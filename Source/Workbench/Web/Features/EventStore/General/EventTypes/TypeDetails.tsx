// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { IDetailsComponentProps } from 'Components';
import { JSONSchemaEditor, JSONSchemaType } from 'Components';
import { EventTypeRegistration } from 'Api/Events';
import { Register } from 'Api/Events';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';

export const TypeDetails = (props: IDetailsComponentProps<EventTypeRegistration>) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [isEditMode, setIsEditMode] = useState(false);
    const [schema, setSchema] = useState<JSONSchemaType>(() => JSON.parse(props.item.schema));
    const [register] = Register.use();

    const handleEdit = () => {
        setIsEditMode(true);
    };

    const handleCancel = () => {
        setIsEditMode(false);
        setSchema(JSON.parse(props.item.schema));
    };

    const handleSave = async () => {
        register.eventStore = params.eventStore!;
        register.types = [{
            type: props.item.type,
            schema: JSON.stringify(schema, null, 2)
        }];

        await register.execute();
        setIsEditMode(false);
    };

    const handleSchemaChange = (newSchema: JSONSchemaType) => {
        setSchema(newSchema);
    };

    return (
        <div className="type-details" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <JSONSchemaEditor
                schema={schema}
                eventTypeName={props.item.type.id}
                isEditMode={isEditMode}
                onChange={handleSchemaChange}
                onSave={handleSave}
                onCancel={handleCancel}
                onEdit={handleEdit}
            />
        </div>
    );
};
