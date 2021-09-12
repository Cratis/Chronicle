// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApolloProvider, ApolloClient, HttpLink, InMemoryCache } from '@apollo/client';
import ReactDOM from 'react-dom';

import { App } from './App';

import './Styles/theme';
import './index.scss';

const cache = new InMemoryCache();
const link = new HttpLink({
    uri: '/graphql/'
})

const client = new ApolloClient({
    cache,
    link,
    name: 'Cratis Management UI',
    defaultOptions: {
        mutate: {
            fetchPolicy: 'no-cache'
        },
        query: {
            fetchPolicy: 'no-cache'
        },
        watchQuery: {
            fetchPolicy: 'no-cache'
        }
    }
});

ReactDOM.render(
    <ApolloProvider client={client}>
        <App />
    </ApolloProvider>,
    document.getElementById('root')
);
