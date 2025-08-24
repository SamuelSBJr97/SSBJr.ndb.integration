using Docker.DotNet;
using Docker.DotNet.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SSBJr.ndb.integration.Web.Web.Services
{
    public class DockerService
    {
        private readonly DockerClient _client;

        public DockerService(string dockerUri = "unix:///var/run/docker.sock")
        {
            _client = new DockerClientConfiguration(new System.Uri(dockerUri)).CreateClient();
        }

        public async Task<IList<ContainerListResponse>> ListContainersAsync()
        {
            return await _client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
        }

        public async Task CreateContainerAsync(string image, string name, IDictionary<string, string> env = null, IDictionary<string, string> ports = null)
        {
            var config = new CreateContainerParameters
            {
                Image = image,
                Name = name,
                Env = env != null ? new List<string>(env.Select(e => $"{e.Key}={e.Value}")) : null,
                ExposedPorts = ports != null ? ports.ToDictionary(p => p.Key, p => new EmptyStruct()) : null,
                HostConfig = new HostConfig
                {
                    PortBindings = ports != null ? ports.ToDictionary(p => p.Key, p => (IList<PortBinding>)new List<PortBinding> { new PortBinding { HostPort = p.Value } }) : null
                }
            };
            await _client.Containers.CreateContainerAsync(config);
        }

        public async Task StartContainerAsync(string id)
        {
            await _client.Containers.StartContainerAsync(id, null);
        }

        public async Task StopContainerAsync(string id)
        {
            await _client.Containers.StopContainerAsync(id, new ContainerStopParameters());
        }

        public async Task RemoveContainerAsync(string id)
        {
            await _client.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters { Force = true });
        }
    }
}
