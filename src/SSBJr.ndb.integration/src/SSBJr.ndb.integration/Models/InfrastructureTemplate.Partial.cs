using System;
using SSBJr.ndb.integration.Models;

namespace SSBJr.ndb.integration.Models;

public partial class InfrastructureTemplate
{
    public string DatabaseTypeDisplay => Configuration is InfrastructureConfig config && config.Database != null ? config.Database.Type.ToString() : "Unknown";
    public string MessagingTypeDisplay => Configuration is InfrastructureConfig config && config.Messaging != null ? config.Messaging.Type.ToString() : "Unknown";
    public string CacheTypeDisplay => Configuration is InfrastructureConfig config && config.Cache != null ? config.Cache.Type.ToString() : "Unknown";
}
