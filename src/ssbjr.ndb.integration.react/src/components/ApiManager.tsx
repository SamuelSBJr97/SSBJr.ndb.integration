import React, { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import './ApiManager.css';

interface ApiDefinition {
  id: string;
  name: string;
  description: string;
  swaggerJson: string;
  baseUrl: string;
  status: 'Created' | 'Deploying' | 'Running' | 'Failed' | 'Stopped';
  createdAt: string;
  lastHealthCheck?: string;
  errorMessage?: string;
  metadata: Record<string, any>;
}

interface ApiDeploymentRequest {
  name: string;
  description: string;
  swaggerJson: string;
  configuration: Record<string, any>;
}

interface Notification {
  title: string;
  message: string;
  type: 'success' | 'warning' | 'error' | 'info';
  timestamp: Date;
}

export const ApiManager: React.FC = () => {
  const [apis, setApis] = useState<ApiDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  const [newApi, setNewApi] = useState<ApiDeploymentRequest>({
    name: '',
    description: '',
    swaggerJson: '',
    configuration: {}
  });

  useEffect(() => {
    loadApis();
    setupSignalR();

    return () => {
      if (connection) {
        connection.stop();
      }
    };
  }, []);

  const setupSignalR = async () => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:8080/notifications')
      .build();

    newConnection.on('ApiStatusChanged', (data: any) => {
      addNotification('Status da API', `API ${data.apiId} mudou para ${data.status}`, 'info');
      loadApis();
    });

    newConnection.on('ApiHealthCheck', (data: any) => {
      const type = data.isHealthy ? 'success' : 'warning';
      addNotification('Verificação de Saúde', 
        `API ${data.apiId}: ${data.isHealthy ? 'Saudável' : 'Com problemas'}`, type);
    });

    newConnection.on('Error', (data: any) => {
      addNotification('Erro', data.message, 'error');
    });

    try {
      await newConnection.start();
      setConnection(newConnection);
    } catch (err) {
      console.error('Erro ao conectar ao SignalR:', err);
    }
  };

  const loadApis = async () => {
    try {
      const response = await fetch('http://localhost:8080/api/apis');
      if (!response.ok) {
        throw new Error('Falha ao carregar APIs');
      }
      const data = await response.json();
      setApis(data);
    } catch (err) {
      setError('Erro ao carregar APIs: ' + (err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file && file.type === 'application/json') {
      const reader = new FileReader();
      reader.onload = (e) => {
        try {
          const content = e.target?.result as string;
          JSON.parse(content); // Validar JSON
          setNewApi(prev => ({ ...prev, swaggerJson: content }));
          setError(null);
        } catch (err) {
          setError('Arquivo JSON inválido');
        }
      };
      reader.readAsText(file);
    } else {
      setError('Por favor, selecione um arquivo JSON válido');
    }
  };

  const createApi = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!newApi.name || !newApi.swaggerJson) {
      setError('Nome e arquivo Swagger são obrigatórios');
      return;
    }

    setUploading(true);
    setError(null);

    try {
      const response = await fetch('http://localhost:8080/api/apis', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(newApi),
      });

      if (!response.ok) {
        const errorData = await response.text();
        throw new Error(errorData);
      }

      setNewApi({ name: '', description: '', swaggerJson: '', configuration: {} });
      loadApis();
      addNotification('Sucesso', 'API criada com sucesso!', 'success');
    } catch (err) {
      setError('Erro ao criar API: ' + (err as Error).message);
    } finally {
      setUploading(false);
    }
  };

  const controlApi = async (id: string, action: 'start' | 'stop') => {
    try {
      await fetch(`http://localhost:8080/api/apis/${id}/${action}`, { method: 'POST' });
      loadApis();
    } catch (err) {
      addNotification('Erro', `Erro ao ${action === 'start' ? 'iniciar' : 'parar'} API`, 'error');
    }
  };

  const checkHealth = async (id: string) => {
    try {
      await fetch(`http://localhost:8080/api/apis/${id}/health`);
      addNotification('Info', 'Verificação de saúde iniciada', 'info');
    } catch (err) {
      addNotification('Erro', 'Erro ao verificar saúde da API', 'error');
    }
  };

  const deleteApi = async (id: string) => {
    if (window.confirm('Tem certeza que deseja deletar esta API?')) {
      try {
        await fetch(`http://localhost:8080/api/apis/${id}`, { method: 'DELETE' });
        loadApis();
        addNotification('Sucesso', 'API deletada com sucesso', 'success');
      } catch (err) {
        addNotification('Erro', 'Erro ao deletar API', 'error');
      }
    }
  };

  const addNotification = (title: string, message: string, type: Notification['type']) => {
    const notification: Notification = {
      title,
      message,
      type,
      timestamp: new Date()
    };

    setNotifications(prev => [...prev.slice(-9), notification]);
  };

  const getStatusBadgeClass = (status: ApiDefinition['status']) => {
    const baseClass = 'status-badge';
    switch (status) {
      case 'Running': return `${baseClass} status-running`;
      case 'Deploying': return `${baseClass} status-deploying`;
      case 'Failed': return `${baseClass} status-failed`;
      case 'Stopped': return `${baseClass} status-stopped`;
      default: return `${baseClass} status-created`;
    }
  };

  if (loading) {
    return (
      <div className="api-manager">
        <div className="loading">
          <div className="spinner"></div>
          <p>Carregando APIs...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="api-manager">
      <div className="container">
        <header className="header">
          <h1>?? Gerenciador de APIs</h1>
          <p>Faça upload de arquivos Swagger JSON para hospedar APIs automaticamente</p>
        </header>

        <div className="main-content">
          <div className="sidebar">
            <div className="card">
              <div className="card-header">
                <h3>?? Nova API</h3>
              </div>
              <div className="card-body">
                <form onSubmit={createApi}>
                  <div className="form-group">
                    <label>Nome da API</label>
                    <input
                      type="text"
                      value={newApi.name}
                      onChange={(e) => setNewApi(prev => ({ ...prev, name: e.target.value }))}
                      placeholder="Ex: Minha API"
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label>Descrição</label>
                    <textarea
                      value={newApi.description}
                      onChange={(e) => setNewApi(prev => ({ ...prev, description: e.target.value }))}
                      placeholder="Descrição da API"
                      rows={3}
                    />
                  </div>

                  <div className="form-group">
                    <label>Arquivo Swagger JSON</label>
                    <input
                      type="file"
                      accept=".json"
                      onChange={handleFileUpload}
                      required
                    />
                  </div>

                  {error && (
                    <div className="alert alert-error">
                      ?? {error}
                    </div>
                  )}

                  <button type="submit" disabled={uploading} className="btn btn-primary">
                    {uploading ? '? Criando...' : '? Criar API'}
                  </button>
                </form>
              </div>
            </div>
          </div>

          <div className="content">
            <div className="card">
              <div className="card-header">
                <h3>?? APIs Hospedadas</h3>
                <button onClick={loadApis} className="btn btn-secondary">
                  ?? Atualizar
                </button>
              </div>
              <div className="card-body">
                {apis.length === 0 ? (
                  <div className="empty-state">
                    <div className="empty-icon">??</div>
                    <p>Nenhuma API hospedada ainda.</p>
                  </div>
                ) : (
                  <div className="apis-list">
                    {apis.map(api => (
                      <div key={api.id} className="api-card">
                        <div className="api-header">
                          <div className="api-info">
                            <h4>{api.name}</h4>
                            <p>{api.description}</p>
                          </div>
                          <span className={getStatusBadgeClass(api.status)}>
                            {api.status}
                          </span>
                        </div>
                        
                        <div className="api-details">
                          <div className="detail">
                            <strong>Criado:</strong> {new Date(api.createdAt).toLocaleString('pt-BR')}
                          </div>
                          {api.lastHealthCheck && (
                            <div className="detail">
                              <strong>Última verificação:</strong> {new Date(api.lastHealthCheck).toLocaleString('pt-BR')}
                            </div>
                          )}
                          {api.errorMessage && (
                            <div className="detail error">
                              <strong>Erro:</strong> {api.errorMessage}
                            </div>
                          )}
                        </div>

                        <div className="api-actions">
                          {api.status === 'Running' && (
                            <button onClick={() => controlApi(api.id, 'stop')} className="btn btn-warning">
                              ?? Parar
                            </button>
                          )}
                          {api.status === 'Stopped' && (
                            <button onClick={() => controlApi(api.id, 'start')} className="btn btn-success">
                              ?? Iniciar
                            </button>
                          )}
                          <button onClick={() => checkHealth(api.id)} className="btn btn-info">
                            ?? Verificar Saúde
                          </button>
                          <button onClick={() => deleteApi(api.id)} className="btn btn-danger">
                            ??? Deletar
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Notificações */}
      <div className="notifications">
        {notifications.slice(-5).map((notification, index) => (
          <div key={index} className={`notification notification-${notification.type}`}>
            <div className="notification-header">
              <strong>{notification.title}</strong>
              <span className="notification-time">
                {notification.timestamp.toLocaleTimeString('pt-BR')}
              </span>
            </div>
            <div className="notification-message">
              {notification.message}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};