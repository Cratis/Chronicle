// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';
import { Page } from 'Components/Common/Page';

export const Sequences = () => {
    const [data, setData] = useState(null);

    useEffect(() => {
        fetch('https://api.example.com/data')
            .then((response) => response.json())
            .then((json) => setData(json))
            .catch((error) => {
                const customErrorEvent = new CustomEvent('customErrorEvent', {
                    detail: { message: error.message },
                });
                window.dispatchEvent(customErrorEvent);
            });
    }, []);

    return (
        <Page title='Sequences'>
            {data ? <pre>{JSON.stringify(data, null, 2)}</pre> : 'Loading...'}
        </Page>
    );
};
