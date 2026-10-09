import React from 'react';
import { Link } from 'react-router-dom';

const NotFound: React.FC = () => {
    return (
        <div className="mt-5">
            <p className="eyebrow mb-1">Page not found</p>
            <h1>This page flew the nest.</h1>
            <p className="mb-4">The page you are looking for does not exist or has moved.</p>
            <Link className="btn btn-primary" to="/">Back to the home page</Link>
        </div>
    );
};

export default NotFound;
