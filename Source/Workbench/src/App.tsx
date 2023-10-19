import { Layout } from "./Layout/Layout";
import { useColorScheme } from "./Utils/useColorScheme";

function App() {
    useColorScheme();
    return (
        <Layout>app content goes here</Layout>
    );
}

export default App;
