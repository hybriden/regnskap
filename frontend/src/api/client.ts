import axios from 'axios';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30_000,
});

// Request-interceptor: legg til auth-token om tilgjengelig
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('auth_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// Response-interceptor: hent data og haandter feil
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      const { status } = error.response;
      if (status === 401) {
        // TODO: Redirect til innlogging
        console.error('Ikke autentisert - vennligst logg inn');
      } else if (status === 403) {
        console.error('Ingen tilgang');
      }
    } else if (error.request) {
      console.error('Ingen respons fra server - sjekk nettverkstilkobling');
    }
    return Promise.reject(error);
  },
);

export default apiClient;
