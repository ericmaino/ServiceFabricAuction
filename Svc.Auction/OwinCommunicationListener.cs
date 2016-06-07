using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Owin;

namespace SFAuction.Svc.Auction
{
    internal sealed class OwinCommunicationListener : ICommunicationListener
    {
        private readonly Action<IAppBuilder, string> startup;
        private readonly ServiceContext serviceContext;
        private readonly string endpointName;
        private IDisposable webApp;
        private string publishAddress;
        private string listeningAddress;

        public OwinCommunicationListener(Action<IAppBuilder, string> startup, ServiceContext serviceContext, string endpointName)
        {
            if (startup == null)
            {
                throw new ArgumentNullException(nameof(startup));
            }

            if (serviceContext == null)
            {
                throw new ArgumentNullException(nameof(serviceContext));
            }

            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }

            this.startup = startup;
            this.serviceContext = serviceContext;
            this.endpointName = endpointName;
        }

        public bool ListenOnSecondary { get; set; }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var serviceEndpoint = this.serviceContext.CodePackageActivationContext.GetEndpoint(this.endpointName);
            int port = serviceEndpoint.Port;
            string routePrefix = null;

            if (this.serviceContext is StatefulServiceContext)
            {
                StatefulServiceContext statefulServiceContext = this.serviceContext as StatefulServiceContext;

                var partition = statefulServiceContext.PartitionId;
                var replica = statefulServiceContext.ReplicaId;
                routePrefix = $"{partition}/{replica}/{Guid.NewGuid()}";
                listeningAddress = $"http://+:{port}/{routePrefix}/";
            }
            else if (this.serviceContext is StatelessServiceContext)
            {
                listeningAddress = $"http://+:{port}";
            }
            else
            {
                throw new InvalidOperationException();
            }

            this.publishAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            try
            {
                this.webApp = WebApp.Start(this.listeningAddress, appBuilder => startup(appBuilder, routePrefix));
                return Task.FromResult(this.publishAddress);
            }
            catch (Exception ex)
            {
                this.StopWebServer();
                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            this.StopWebServer();
            return Task.FromResult(true);
        }

        public void Abort()
        {
            this.StopWebServer();
        }

        private void StopWebServer()
        {
            if (this.webApp != null)
            {
                try
                {
                    this.webApp.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // no-op
                }
            }
        }
    }
}
