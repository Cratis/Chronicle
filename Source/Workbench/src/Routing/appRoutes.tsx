import { RouteDefinition } from './RouteDefinition';
import { Test } from '../TestData/Test';
import App from '../App';
import { postLoader } from '../TestData/loader';

export const appRoutes: RouteDefinition[] = [
    {
        title: 'Home',
        path: '/',
        element: <App />,
    },
    {
        title: 'Test',
        path: '/test',
        loader: postLoader,
        element: <Test />,
        errorElement: <>This is not working</>,
    },
];
