// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllMicroservices } from 'API/configuration/microservices/AllMicroservices';
import { NavigationPage } from '../Components/Navigation/NavigationPage';
import * as icons from '@mui/icons-material';
import { NavigationItem, NavigationButton } from '../Components/Navigation';
import { EventTypes } from './EventTypes';
import { FailedPartitions } from './FailedPartitions';
import { Observers } from './Observers';
import { Projections } from './Projections';
import { EventSequences } from './EventSequences';
import { PivotViewer } from './PivotViewer';

export const EventStore = () => {
    const [microservices] = AllMicroservices.use();

    const navigationItems = microservices.data.map(microservice => {
        return {
            title: microservice.name,
            icon: <icons.Cabin />,
            targetPath: microservice.id,
            routePath: ':microserviceId',
            children: [{
                title: 'Types',
                icon: <icons.DataObject />,
                targetPath: 'types',
                content: <EventTypes />
            }, {
                title: 'Sequences',
                icon: <icons.Stream />,
                targetPath: 'sequences',
                content: <EventSequences />
            }, {
                title: 'Failed partitions',
                icon: <icons.ErrorOutline />,
                targetPath: 'failed-partitions',
                content: <FailedPartitions />
            }, {
                title: 'Observers',
                icon: <icons.LoupeOutlined />,
                targetPath: 'observers',
                content: <Observers />
            }, {
                title: 'Projections',
                icon: <icons.Mediation />,
                targetPath: 'projections',
                content: <Projections />
            }, {
                title: 'Pivot viewer',
                icon: <icons.Mediation />,
                targetPath: 'pivot-viewer',
                content: <PivotViewer />
            }]
        } as NavigationItem;
    });

    return (
        <NavigationPage navigationItems={navigationItems}/>
    );
};
