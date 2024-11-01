// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CardFooter } from 'Components/Common/CardFooter';
import { StoreCard } from 'Components/Common/StoreCard';
import { AllEventStores } from 'Api/EventStores/AllEventStores';

export const Home = () => {
    const [eventStores] = AllEventStores.use();

    return (
        <div>
            <h1 className='text-4xl m-3'>Select Event Store</h1>
            <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-2 '>
                {eventStores.data.map((eventStore) => {
                    return (
                        <>
                            <StoreCard
                                title={eventStore.name}
                                path={`/event-store/${eventStore.name}/Default`}
                                footer={<CardFooter />}
                                description={eventStore.description}
                            />
                        </>
                    );
                })}
            </div>
        </div >
    );
};
