using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SFAuction.Common;
using SFAuction.JsonRpc;
using System.Web.Script.Serialization;
using Richter.Utilities;
using Newtonsoft.Json;
using System.IO;
using System.Net;

namespace SFAuction.OperationsProxy
{
    public abstract class OperationProxy
    {
        private static readonly JavaScriptSerializer m_serializer = new JavaScriptSerializer();
        private static readonly HttpClient s_httpClient =
           new HttpClient(new CircuitBreakerHttpMessageHandler(3, TimeSpan.FromSeconds(30)));
        public readonly Resolver m_resolver;
        protected OperationProxy(Resolver resolver, params JavaScriptConverter[] converters)
        {
            m_serializer.RegisterConverters(converters);
            m_resolver = resolver;
        }
        private async Task<JsonRpcResponse> SendJsonRpcAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            // Send request to server:
            String response = await m_resolver.CallAsync(cancellationToken,
               async (ep, ct) =>
               {
                   var result = await s_httpClient.GetAsync(ep + $"?jsonrpc={request.ToString()}").WithCancellation(cancellationToken).ConfigureAwait(false);
                   result.ThrowIfNotOk();
                   return await result.Content.ReadAsStringAsync();
               });

            // Get response from server:
            return JsonRpcResponse.Parse(response);
        }

        protected async Task SendAsync(String method, IDictionary<String, Object> parameters, CancellationToken cancellationToken)
        {
            JsonRpcRequest jsonRpcRequest = new JsonRpcRequest(Guid.NewGuid().ToString(), method, parameters);
            JsonRpcResponse jsonRpcResponse = await SendJsonRpcAsync(jsonRpcRequest, cancellationToken);
            var result = jsonRpcResponse.JsonResult;
        }
        protected async Task SendAsync(String method, IList<Object> parameters, CancellationToken cancellationToken)
        {
            JsonRpcRequest jsonRpcRequest = new JsonRpcRequest(Guid.NewGuid().ToString(), method, parameters);
            JsonRpcResponse jsonRpcResponse = await SendJsonRpcAsync(jsonRpcRequest, cancellationToken);
            var result = jsonRpcResponse.JsonResult;
        }
        protected async Task<TResult> SendAsync<TResult>(String method, IDictionary<String, Object> parameters, CancellationToken cancellationToken)
        {
            JsonRpcRequest jsonRpcRequest = new JsonRpcRequest(Guid.NewGuid().ToString(), method, parameters);
            JsonRpcResponse jsonRpcResponse = await SendJsonRpcAsync(jsonRpcRequest, cancellationToken);
            return m_serializer.Deserialize<TResult>(jsonRpcResponse.JsonResult);
        }
        protected async Task<TResult> SendAsync<TResult>(String method, IList<Object> parameters, CancellationToken cancellationToken)
        {
            JsonRpcRequest jsonRpcRequest = new JsonRpcRequest(Guid.NewGuid().ToString(), method, parameters);
            JsonRpcResponse jsonRpcResponse = await SendJsonRpcAsync(jsonRpcRequest, cancellationToken);
            return m_serializer.Deserialize<TResult>(jsonRpcResponse.JsonResult);
        }
    }

    public static class ResultExtensions
    {
        public static void ThrowIfNotOk(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var webex = new WebException("Service Unavailable", WebExceptionStatus.ConnectFailure);
                throw new HttpRequestException("Ba", webex);
            }
        }
    }

    public sealed class ExperimentDecorator : InternetAuctionOperationProxy
    {
        private readonly Resolver m_api;
        private bool UseApi { get; set; }

        public ExperimentDecorator(Resolver original, Resolver api)
            : base(original)
        {
            m_api = api;
            UseApi = true;
        }

        public override Task<ItemInfo[]> GetAuctionItemsAsync(CancellationToken cancellationToken)
        {
            if (UseApi)
            {
                return m_api.CallAsync(cancellationToken, async (ep, ct) =>
                {
                    HttpClient http = new HttpClient();
                    var content = await http.GetStringAsync(new Uri(new Uri(ep), "api/Items"));
                    return JsonConvert.DeserializeObject<ItemInfo[]>(content);
                });
            }

            return base.GetAuctionItemsAsync(cancellationToken);
        }
    }

    public class InternetAuctionOperationProxy : OperationProxy, IInternetOperations
    {
        public InternetAuctionOperationProxy(Resolver resolver)
           : base(resolver, new DataTypeJsonConverter())
        {
        }

        public Task<UserInfo> CreateUserAsync(String userEmail, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<String, Object>
            {
                [nameof(userEmail)] = userEmail,
            };
            return SendAsync<UserInfo>(nameof(CreateUserAsync), parameters, cancellationToken);
        }
        public Task<UserInfo> GetUserAsync(String userEmail, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<String, Object>
            {
                [nameof(userEmail)] = userEmail,
            };
            return SendAsync<UserInfo>(nameof(GetUserAsync), parameters, cancellationToken);
        }

        public Task<ItemInfo> CreateItemAsync(String sellerEmail, String itemName, String imageUrl, DateTime expiration, Decimal startAmount, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<String, Object>
            {
                [nameof(sellerEmail)] = sellerEmail,
                [nameof(itemName)] = itemName,
                [nameof(imageUrl)] = imageUrl,
                [nameof(expiration)] = expiration,
                [nameof(startAmount)] = startAmount
            };
            return SendAsync<ItemInfo>(nameof(CreateItemAsync), parameters, cancellationToken);
        }

        public Task<Bid[]> PlaceBidAsync(String bidderEmail, String sellerEmail, String itemName, Decimal bidAmount, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<String, Object>
            {
                [nameof(bidderEmail)] = bidderEmail,
                [nameof(sellerEmail)] = sellerEmail,
                [nameof(itemName)] = itemName,
                [nameof(bidAmount)] = bidAmount
            };
            return SendAsync<Bid[]>(nameof(PlaceBidAsync), parameters, cancellationToken);
        }

        public Task<Bid> PlaceBid2Async(String bidderEmail, String sellerEmail, String itemName, Decimal bidAmount, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<String, Object>
            {
                [nameof(bidderEmail)] = bidderEmail,
                [nameof(sellerEmail)] = sellerEmail,
                [nameof(itemName)] = itemName,
                [nameof(bidAmount)] = bidAmount
            };
            return SendAsync<Bid>(nameof(PlaceBid2Async), parameters, cancellationToken);
        }

        public Task<ItemInfo[]> GetItemsBiddingAsync(String userEmail, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<String, Object>
            {
                [nameof(userEmail)] = userEmail
            };
            return SendAsync<ItemInfo[]>(nameof(GetItemsBiddingAsync), parameters, cancellationToken);
        }

        public Task<ItemInfo[]> GetItemsSellingAsync(String userEmail, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<String, Object>
            {
                [nameof(userEmail)] = userEmail
            };
            return SendAsync<ItemInfo[]>(nameof(GetItemsSellingAsync), parameters, cancellationToken);
        }

        public virtual Task<ItemInfo[]> GetAuctionItemsAsync(CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<String, Object> { };
            return SendAsync<ItemInfo[]>(nameof(GetAuctionItemsAsync), parameters, cancellationToken);
        }
    }

    public sealed class InternalAuctionOperationProxy : OperationProxy, IInternalOperations
    {
        public InternalAuctionOperationProxy(Resolver resolver) : base(resolver, new DataTypeJsonConverter())
        {
        }

        public Task<Bid[]> PlaceBid2Async(String bidderEmail, String sellerEmail, String itemName, Decimal bidAmount, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<String, Object>
            {
                [nameof(bidderEmail)] = bidderEmail,
                [nameof(sellerEmail)] = sellerEmail,
                [nameof(itemName)] = itemName,
                [nameof(bidAmount)] = bidAmount
            };
            return SendAsync<Bid[]>(nameof(PlaceBid2Async), parameters, cancellationToken);
        }
    }
}