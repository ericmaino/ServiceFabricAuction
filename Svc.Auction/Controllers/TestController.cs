using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace SFAuction.Svc.Auction.Controllers
{
    public sealed class TestController : ApiController
    {
        [HttpGet]
        public IHttpActionResult GetValue()
        {
            return Ok("HelloWorld");
        }
    }
}
