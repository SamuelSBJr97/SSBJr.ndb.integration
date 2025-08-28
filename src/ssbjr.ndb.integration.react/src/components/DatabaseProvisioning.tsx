import React, { useState, useEffect } from 'react';
import './DatabaseProvisioning.css';

interface DatabaseProvider {
  name: string;
  types: string[];
  regions: string[];
}

interface DatabaseRequest {
  provider: string;
  databaseType: string;
  name: string;
  region: string;
}

interface ProvisioningResult {
  id: string;
  status: string;
  provider: string;
  databaseType: string;
  name: string;
  region: string;
}

export const DatabaseProvisioning: React.FC = () => {
  const [providers, setProviders] = useState<string[]>([]);
  const [databaseTypes, setDatabaseTypes] = useState<string[]>([]);
  const [regions] = useState<string[]>(['us-east-1', 'us-west-2', 'eu-west-1', 'ap-southeast-1']);
  const [formData, setFormData] = useState<DatabaseRequest>({
    provider: '',
    databaseType: '',
    name: '',
    region: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<ProvisioningResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchProviders();
  }, []);

  const fetchProviders = async () => {
    try {
      const response = await fetch('/api/providers');
      if (!response.ok) {
        throw new Error('Falha ao buscar provedores');
      }
      const data = await response.json();
      setProviders(data.providers || []);
      setDatabaseTypes(data.databaseTypes || []);
    } catch (err) {
      setError('Erro ao carregar provedores: ' + (err as Error).message);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);
    setResult(null);

    try {
      const response = await fetch('/api/databases', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData),
      });

      if (!response.ok) {
        throw new Error('Falha ao provisionar banco de dados');
      }

      const data = await response.json();
      setResult(data);
    } catch (err) {
      setError('Erro ao provisionar banco de dados: ' + (err as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="database-provisioning">
      <div className="container">
        <h1>SSBJr.ndb.Integration</h1>
        <p className="subtitle">Provisionamento Unificado de Bancos de Dados</p>

        <form onSubmit={handleSubmit} className="provisioning-form">
          <div className="form-group">
            <label htmlFor="provider">Provedor de Nuvem:</label>
            <select
              id="provider"
              name="provider"
              value={formData.provider}
              onChange={handleInputChange}
              required
            >
              <option value="">Selecione um provedor</option>
              {providers.map(provider => (
                <option key={provider} value={provider}>{provider}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="databaseType">Tipo de Banco de Dados:</label>
            <select
              id="databaseType"
              name="databaseType"
              value={formData.databaseType}
              onChange={handleInputChange}
              required
            >
              <option value="">Selecione um tipo</option>
              {databaseTypes.map(type => (
                <option key={type} value={type}>{type}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="name">Nome do Banco de Dados:</label>
            <input
              type="text"
              id="name"
              name="name"
              value={formData.name}
              onChange={handleInputChange}
              placeholder="Digite o nome do banco de dados"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="region">Região:</label>
            <select
              id="region"
              name="region"
              value={formData.region}
              onChange={handleInputChange}
              required
            >
              <option value="">Selecione uma região</option>
              {regions.map(region => (
                <option key={region} value={region}>{region}</option>
              ))}
            </select>
          </div>

          <button type="submit" disabled={isLoading} className="submit-button">
            {isLoading ? 'Provisionando...' : 'Provisionar Banco de Dados'}
          </button>
        </form>

        {error && (
          <div className="error-message">
            <h3>? Erro</h3>
            <p>{error}</p>
          </div>
        )}

        {result && (
          <div className="success-message">
            <h3>? Provisionamento Iniciado</h3>
            <div className="result-details">
              <p><strong>ID:</strong> {result.id}</p>
              <p><strong>Status:</strong> {result.status}</p>
              <p><strong>Provedor:</strong> {result.provider}</p>
              <p><strong>Tipo:</strong> {result.databaseType}</p>
              <p><strong>Nome:</strong> {result.name}</p>
              <p><strong>Região:</strong> {result.region}</p>
            </div>
          </div>
        )}

        <div className="api-docs">
          <p>
            <a href="/swagger" target="_blank" rel="noopener noreferrer">
              ?? Documentação da API (Swagger)
            </a>
          </p>
        </div>
      </div>
    </div>
  );
};