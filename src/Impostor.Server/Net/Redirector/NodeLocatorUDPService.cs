using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Impostor.Server.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Impostor.Server.Net.Redirector
{
    public class NodeLocatorUDPService : BackgroundService
    {
        private readonly NodeLocatorUDP _nodeLocator;
        private readonly ILogger<NodeLocatorUDPService> _logger;
        private readonly UdpClient _client;

        public NodeLocatorUDPService(
            INodeLocator nodeLocator, 
            ILogger<NodeLocatorUDPService> logger, 
            IOptions<ServerRedirectorConfig> options)
        {
            _nodeLocator = (NodeLocatorUDP) nodeLocator;
            _logger = logger;

            if (!IPEndPoint.TryParse(options.Value.Locator.UdpMasterEndpoint, out var endpoint))
            {
                throw new ArgumentException("UdpMasterEndpoint should be in the ip:port format.");
            }

            _client = new UdpClient(endpoint)
            {
                DontFragment = true
            };
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning("Master server is listening for node updates on {0}.", _client.Client.LocalEndPoint);
            
            stoppingToken.Register(() =>
            {
                _client.Close();
                _client.Dispose();
            });

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Receive data from a node.
                    UdpReceiveResult data;
                    
                    try
                    {
                        data = await _client.ReceiveAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    
                    // Check if data is valid.
                    if (data.Buffer.Length == 0)
                    {
                        break;
                    }

                    // Parse the data.
                    var message = Encoding.UTF8.GetString(data.Buffer);
                    var parts = message.Split(',', 2);
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    if (!IPEndPoint.TryParse(parts[1], out var ipEndPoint))
                    {
                        continue;
                    }

                    // Update the NodeLocator.
                    _nodeLocator.Update(ipEndPoint, parts[0]);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in NodeLocatorUDPService.");
            }
            
            _logger.LogWarning("Master server node update listener is stopping.");
        }
    }
}