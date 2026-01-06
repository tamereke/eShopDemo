import axios from 'axios';

// Gateway URL (AppHost will inject this but for local dev we can default)
const API_BASE_URL = '/api';

const api = axios.create({
  baseURL: API_BASE_URL,
});

export const getProducts = () => api.get('/products').then(res => res.data);
export const getCategories = () => api.get('/categories').then(res => res.data);
export const getBasket = (customerId) => api.get(`/basket/${customerId}`).then(res => res.data);
export const updateBasket = (basket) => api.post('/basket', basket).then(res => res.data);
export const createOrder = (order) => api.post('/orders', order).then(res => res.data);
export const getOrder = (id) => api.get(`/orders/${id}`).then(res => res.data);
export const getOrdersByCustomer = (customerId) => api.get(`/orders/customer/${customerId}`).then(res => res.data);
export const getPendingOrders = () => api.get('/orders/pending').then(res => res.data);
export const getOrdersByStatus = (status) => api.get(`/orders/status/${status}`).then(res => res.data);
export const approveOrder = (id) => api.put(`/orders/${id}/approve`).then(res => res.data);
export const cancelOrder = (id) => api.put(`/orders/${id}/cancel`).then(res => res.data);
export const deleteBasket = (customerId) => api.delete(`/basket/${customerId}`).then(res => res.data);

export default api;
