// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllMicroservices } from 'API/configuration/microservices/AllMicroservices';
import { NavigationPage } from '../Components/Navigation/NavigationPage';
import * as icons from '@mui/icons-material';
import { useNavigate, Routes, Route } from 'react-router-dom';
import { NavigationItem, NavigationRootItem } from '../Components/Navigation';
import { EventTypes } from './EventTypes';
import { useRouteParams } from './RouteParams';

type NavigationItem = {
    title: string;
    icon: JSX.Element;
    target: string
    element: JSX.Element;
}

const items: NavigationItem[] = [{
    title: 'Types',
    icon: <icons.DataObject />,
    target: 'types',
    element: <EventTypes />
}];

export const EventStore = () => {
    const navigate = useNavigate();
    const [microservices] = AllMicroservices.use();


    return (
        <NavigationPage>
            <NavigationPage.Navigation>
                <>
                    {microservices.data.map(microservice => {
                        return (
                            <NavigationRootItem
                                key={microservice.id}
                                title={microservice.name}
                                icon={<icons.Cabin />}
                                onClick={() => navigate(`/event-store/${microservice.id}`)} />
                        );
                    })}

                    <Routes>
                        <Route path=":microserviceId/*" element={
                            <>
                                {items.map((item, index) => {
                                    const params = useRouteParams();
                                    return (
                                        <NavigationItem
                                            key={index}
                                            title={item.title}
                                            icon={item.icon}
                                            onClick={() => navigate(`/event-store/${params.microserviceId}/${item.target}`)} />
                                    );
                                })}
                            </>} />
                    </Routes>
                </>
            </NavigationPage.Navigation>
            <NavigationPage.Content>
                <Routes>
                    <Route path=":microserviceId/types" element={<EventTypes />} />
                </Routes>
            </NavigationPage.Content>
        </NavigationPage >
    );
};
