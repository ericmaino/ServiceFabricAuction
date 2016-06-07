using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;
using Microsoft.Practices.Unity;
using Microsoft.ServiceFabric.Data;

namespace SFAuction.Svc.Auction
{
    internal sealed class StatefulServiceDependencyResolver : DependencyResolverDecorator
    {
        public StatefulServiceDependencyResolver(IReliableStateManager state, IDependencyResolver resolver)
            : base(resolver)
        {
            var container = new UnityContainer();
            container.RegisterInstance(state);
            Container = container;
        }

        private UnityContainer Container { get; }

        public override object GetService(Type serviceType)
        {
            object result = null;

            try
            {
                result = Container.Resolve(serviceType);
            }
            catch (ResolutionFailedException)
            {
                result = base.GetService(serviceType);
            }

            return result;
        }

        public override IDependencyScope BeginScope()
        {
            return this;
        }
    }
}