import React, { useEffect, useState, useRef } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import './Terminal.css';
import { ChevronsDown, ChevronsUp, Activity } from 'lucide-react';

const Terminal = () => {
    const [logs, setLogs] = useState([]);
    const [minimized, setMinimized] = useState(false);
    const [connection, setConnection] = useState(null);
    const logsEndRef = useRef(null);

    useEffect(() => {
        // Connect to SignalR Hub
        // Gateway address is proxied via same origin in dev or specific URL
        // In local Aspire, Gateway is at http://localhost:5000 (usually) or handled by proxy
        // Since Web.UI is running on 5173, we need to point to Gateway.
        // Let's try relative path if proxy is set, or absolute.
        // For Aspire, usually we might need an ENV variable for generic usage, 
        // but let's assume standard Gateway port 5000 or derive from window.location if proxied (unlikely for SPA separate port).

        // Since we don't have a rigid config here, let's try direct Gateway URL for demo.
        // Note: CORS is enabled in Gateway.
        // Use relative URL so Vite proxy handles the port and CORS
        const hubUrl = "/eventhub";

        const newConnection = new HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .build();

        setConnection(newConnection);
    }, []);

    useEffect(() => {
        if (connection) {
            connection.start()
                .then(() => {
                    console.log('Connected to Event Hub');

                    connection.on('ReceiveLog', (log) => {
                        console.log('Terminal: Log received', log);
                        setLogs(prevLogs => [...prevLogs, log]);
                    });
                })
                .catch(e => console.log('Connection failed: ', e));
        }
    }, [connection]);

    useEffect(() => {
        if (logsEndRef.current) {
            logsEndRef.current.scrollIntoView({ behavior: 'smooth' });
        }
    }, [logs]);

    const toggleMinimize = () => {
        setMinimized(!minimized);
    };

    return (
        <div className={`terminal-container ${minimized ? 'minimized' : ''}`}>
            <div className="terminal-header" onClick={toggleMinimize}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <Activity size={16} color="#0f0" />
                    <span>System Events Stream</span>
                </div>
                {minimized ? <ChevronsUp size={16} /> : <ChevronsDown size={16} />}
            </div>
            <div className="terminal-body">
                {logs.length === 0 && <div style={{ color: '#666', fontStyle: 'italic' }}>Waiting for events...</div>}
                {logs.map((log, index) => (
                    <div key={index} className="log-entry">
                        <span className="log-time">{new Date(log.timestamp || log.Timestamp || Date.now()).toLocaleTimeString()}</span>
                        <span className="log-source">[{log.source || log.Source}]</span>
                        <span className="log-message">{log.message || log.Message}</span>
                    </div>
                ))}
                <div ref={logsEndRef} />
            </div>
        </div>
    );
};

export default Terminal;
