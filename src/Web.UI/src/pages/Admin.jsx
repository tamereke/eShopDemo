import { useState, useEffect } from 'react';
import { Plus, Package, Tags, Trash2, ClipboardList, CheckCircle, XCircle } from 'lucide-react';
import api, { getProducts, getCategories, getPendingOrders, approveOrder, cancelOrder } from '../api';

export default function Admin() {
    const [activeTab, setActiveTab] = useState('products');
    const [products, setProducts] = useState([]);
    const [categories, setCategories] = useState([]);
    const [pendingOrders, setPendingOrders] = useState([]);
    const [orderStatusFilter, setOrderStatusFilter] = useState('Pending');

    // Form states
    const [newProduct, setNewProduct] = useState({ name: '', description: '', price: 0, imageUri: '', categoryId: '' });
    const [newCategory, setNewCategory] = useState({ name: '', description: '' });

    useEffect(() => {
        fetchData();
    }, [activeTab, orderStatusFilter]);

    const fetchData = async () => {
        if (activeTab === 'orders') {
            // If status is Pending, use the explicit pending endpoint or the generic status one
            const statusToFetch = orderStatusFilter;
            // The enum in backend is: Pending=0, Confirmed=1 (Approved), Processing=2, Completed=3, Cancelled=4
            // Let's map our UI filter to backend enum strings if needed, but 'Pending' works.
            // For 'Approved', we should probably fetch 'Confirmed'.
            let backendStatus = statusToFetch;
            if (statusToFetch === 'Approved') backendStatus = 'Confirmed';

            try {
                // We added getOrdersByStatus(status)
                const orders = await api.get(`/orders/status/${backendStatus}`).then(res => res.data);
                setPendingOrders(orders || []);
            } catch (e) {
                console.error(e);
                setPendingOrders([]);
            }
        } else {
            const [p, c] = await Promise.all([getProducts(), getCategories()]);
            setProducts(p);
            setCategories(c);
        }
    };

    const handleCreateProduct = async (e) => {
        e.preventDefault();
        await api.post('/products', newProduct);
        setNewProduct({ name: '', description: '', price: 0, imageUri: '', categoryId: '' });
        fetchData();
    };

    const handleCreateCategory = async (e) => {
        e.preventDefault();
        await api.post('/categories', newCategory);
        setNewCategory({ name: '', description: '' });
        fetchData();
    };

    const handleApproveOrder = async (orderId) => {
        try {
            await approveOrder(orderId);
            fetchData();
        } catch (error) {
            console.error('Failed to approve order', error);
            alert('Approval failed');
        }
    };

    const handleCancelOrder = async (orderId) => {
        if (!confirm('Are you sure you want to cancel this order?')) return;
        try {
            await cancelOrder(orderId);
            fetchData();
        } catch (error) {
            console.error('Failed to cancel order', error);
            alert('Cancellation failed');
        }
    };

    return (
        <div className="container">
            <div style={{ display: 'flex', gap: '1rem', marginBottom: '2rem' }}>
                <button onClick={() => setActiveTab('products')} className={`btn ${activeTab === 'products' ? 'btn-primary' : 'btn-outline'}`}>
                    <Package size={18} /> Products
                </button>
                <button onClick={() => setActiveTab('categories')} className={`btn ${activeTab === 'categories' ? 'btn-primary' : 'btn-outline'}`}>
                    <Tags size={18} /> Categories
                </button>
                <button onClick={() => setActiveTab('orders')} className={`btn ${activeTab === 'orders' ? 'btn-primary' : 'btn-outline'}`}>
                    <ClipboardList size={18} /> Order Management
                </button>
            </div>

            {activeTab === 'orders' ? (
                <div className="glass-card">
                    <div style={{ display: 'flex', gap: '1rem', marginBottom: '1.5rem', borderBottom: '1px solid var(--border)', paddingBottom: '1rem' }}>
                        {['Pending', 'Approved', 'Cancelled'].map(status => (
                            <button
                                key={status}
                                onClick={() => setOrderStatusFilter(status)}
                                style={{
                                    background: 'none',
                                    border: 'none',
                                    color: orderStatusFilter === status ? 'var(--primary)' : 'var(--text-muted)',
                                    fontWeight: orderStatusFilter === status ? 'bold' : 'normal',
                                    cursor: 'pointer',
                                    fontSize: '1rem'
                                }}
                            >
                                {status}
                            </button>
                        ))}
                    </div>

                    <h3>{orderStatusFilter} Orders</h3>
                    {pendingOrders.length === 0 ? <p style={{ color: 'var(--text-muted)' }}>No {orderStatusFilter.toLowerCase()} orders.</p> : (
                        <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '1rem' }}>
                            <thead>
                                <tr style={{ textAlign: 'left', borderBottom: '1px solid var(--border)' }}>
                                    <th style={{ padding: '0.5rem' }}>Order ID</th>
                                    <th style={{ padding: '0.5rem' }}>Customer</th>
                                    <th style={{ padding: '0.5rem' }}>Total</th>
                                    <th style={{ padding: '0.5rem' }}>Date</th>
                                    <th style={{ padding: '0.5rem' }}>Action</th>
                                </tr>
                            </thead>
                            <tbody>
                                {pendingOrders.map(order => {
                                    const total = order.items.reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);
                                    return (
                                        <tr key={order.orderId} style={{ borderBottom: '1px solid var(--border)' }}>
                                            <td style={{ padding: '0.8rem 0.5rem', fontSize: '0.9rem' }}>{order.orderId ? order.orderId.substring(0, 8) : 'N/A'}...</td>
                                            <td style={{ padding: '0.8rem 0.5rem' }}>{order.customerId}</td>
                                            <td style={{ padding: '0.8rem 0.5rem', fontWeight: 'bold' }}>${total.toFixed(2)}</td>
                                            <td style={{ padding: '0.8rem 0.5rem', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                                                {new Date(order.createdAt).toLocaleDateString()}
                                            </td>
                                            <td style={{ padding: '0.8rem 0.5rem' }}>
                                                {orderStatusFilter === 'Pending' && (
                                                    <>
                                                        <button
                                                            onClick={() => handleApproveOrder(order.orderId)}
                                                            className="btn btn-primary"
                                                            style={{ padding: '0.4rem 0.8rem', fontSize: '0.8rem', gap: '0.3rem', background: '#22c55e', marginRight: '0.5rem' }}
                                                        >
                                                            <CheckCircle size={14} /> Approve
                                                        </button>
                                                        <button
                                                            onClick={() => handleCancelOrder(order.orderId)}
                                                            className="btn btn-primary"
                                                            style={{ padding: '0.4rem 0.8rem', fontSize: '0.8rem', gap: '0.3rem', background: '#ef4444' }}
                                                        >
                                                            <XCircle size={14} /> Cancel
                                                        </button>
                                                    </>
                                                )}
                                                {orderStatusFilter === 'Approved' && <span style={{ color: '#4ade80' }}>Approved</span>}
                                                {orderStatusFilter === 'Cancelled' && <span style={{ color: '#ef4444' }}>Cancelled</span>}
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    )}
                </div>
            ) : (
                <div style={{ display: 'grid', gridTemplateColumns: '1.5fr 1fr', gap: '3rem' }}>
                    <div className="glass-card">
                        <h3>Current {activeTab === 'products' ? 'Products' : 'Categories'}</h3>
                        <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '1rem' }}>
                            <thead>
                                <tr style={{ textAlign: 'left', borderBottom: '1px solid var(--border)' }}>
                                    <th style={{ padding: '0.5rem' }}>Name</th>
                                    <th style={{ padding: '0.5rem' }}>Details</th>
                                    <th style={{ padding: '0.5rem' }}>Action</th>
                                </tr>
                            </thead>
                            <tbody>
                                {((activeTab === 'products' ? products : categories) || []).map(item => (
                                    <tr key={item.id} style={{ borderBottom: '1px solid var(--border)' }}>
                                        <td style={{ padding: '0.8rem 0.5rem' }}>{item.name}</td>
                                        <td style={{ padding: '0.8rem 0.5rem', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                                            {activeTab === 'products' ? `$${item.price} - ${item.categoryName}` : item.description}
                                        </td>
                                        <td style={{ padding: '0.8rem 0.5rem' }}>
                                            <button style={{ color: '#ef4444', background: 'none', border: 'none', cursor: 'pointer' }}>
                                                <Trash2 size={16} />
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    <div className="glass-card" style={{ height: 'fit-content' }}>
                        <h3>Add New {activeTab === 'products' ? 'Product' : 'Category'}</h3>
                        <form style={{ marginTop: '1.5rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}
                            onSubmit={activeTab === 'products' ? handleCreateProduct : handleCreateCategory}>

                            <input
                                className="input" placeholder="Name" required
                                value={activeTab === 'products' ? newProduct.name : newCategory.name}
                                onChange={e => activeTab === 'products' ?
                                    setNewProduct({ ...newProduct, name: e.target.value }) :
                                    setNewCategory({ ...newCategory, name: e.target.value })
                                }
                            />

                            <textarea
                                className="input" placeholder="Description" rows="3"
                                value={activeTab === 'products' ? newProduct.description : newCategory.description}
                                onChange={e => activeTab === 'products' ?
                                    setNewProduct({ ...newProduct, description: e.target.value }) :
                                    setNewCategory({ ...newCategory, description: e.target.value })
                                }
                            />

                            {activeTab === 'products' && (
                                <>
                                    <input
                                        type="number" className="input" placeholder="Price" step="0.01"
                                        value={newProduct.price} onChange={e => setNewProduct({ ...newProduct, price: parseFloat(e.target.value) })}
                                    />
                                    <input
                                        className="input" placeholder="Image URL"
                                        value={newProduct.imageUri} onChange={e => setNewProduct({ ...newProduct, imageUri: e.target.value })}
                                    />
                                    <select
                                        className="input" required
                                        value={newProduct.categoryId} onChange={e => setNewProduct({ ...newProduct, categoryId: e.target.value })}
                                    >
                                        <option value="">Select Category</option>
                                        {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                                    </select>
                                </>
                            )}

                            <button type="submit" className="btn btn-primary" style={{ marginTop: '1rem', justifyContent: 'center' }}>
                                <Plus size={18} /> Create {activeTab === 'products' ? 'Product' : 'Category'}
                            </button>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
