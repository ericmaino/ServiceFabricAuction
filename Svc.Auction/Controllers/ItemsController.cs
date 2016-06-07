using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ServiceFabric.Data;

namespace SFAuction.Svc.Auction.Controllers
{
    public sealed class ItemsController : ApiController
    {
        private readonly IReliableStateManager m_stateManager;

        public ItemsController(IReliableStateManager stateManager)
        {
            m_stateManager = stateManager;
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetItems()
        {
            var operations = await PartitionOperations.CreateAsync(m_stateManager);
            var result = await operations.GetAuctionItemsAsync(CancellationToken.None);
            return Ok(result);
        }
    }
}
