// axios-config.ts
import axios from 'axios';

// Set base configuration
axios.defaults.baseURL = 'http://localhost:8000';
axios.defaults.withCredentials = true;

// Request interceptor to add auth token to every request
axios.interceptors.request.use(
    (config) => {
        // Get token from localStorage
        const token = localStorage.getItem('jwt_token');
        
        // If token exists, add it to the Authorization header
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Response interceptor to handle auth errors
axios.interceptors.response.use(
    (response) => {
        return response;
    },
    (error) => {
        // If we get a 401 response, the token might be invalid
        if (error.response?.status === 401) {
            // Clear the token and redirect to login
            localStorage.removeItem('jwt_token');
            localStorage.removeItem('user_data');
            localStorage.removeItem('authenticated');
            
            // Only redirect if not already on login page
            if (window.location.pathname !== '/') {
                window.location.href = '/';
            }
        }
        
        return Promise.reject(error);
    }
);

export default axios;
