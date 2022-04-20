// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ScrollableDetailsList } from '@aksio/cratis-fluentui';
import { IColumn } from '@fluentui/react';
import { Microservices as AllMicroservices } from 'API/configuration/Microservices';

const columns: IColumn[] = [
    {
        key: 'id',
        name: 'Id',
        fieldName: 'id',
        minWidth: 240,
        maxWidth: 240
    },
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 200
    }
];


export const Microservices = () => {

    const [allMicroservices] = AllMicroservices.use();

    return (
        <ScrollableDetailsList
            columns={columns}
            items={allMicroservices.data} />
    );
};
