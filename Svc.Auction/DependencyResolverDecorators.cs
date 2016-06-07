using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;

namespace SFAuction.Svc.Auction
{
    public abstract class DependencyResolverDecorator : IDependencyResolver
    {
        protected DependencyResolverDecorator(IDependencyResolver resolver)
        {
            Resolver = resolver;
        }

        private IDependencyResolver Resolver { get; }

        public virtual IDependencyScope BeginScope()
        {
            return Resolver.BeginScope();
        }

        public virtual void Dispose()
        {
            Resolver.Dispose();
        }

        public virtual object GetService(Type serviceType)
        {
            return Resolver.GetService(serviceType);
        }

        public virtual IEnumerable<object> GetServices(Type serviceType)
        {
            return Resolver.GetServices(serviceType);
        }
    }
}
