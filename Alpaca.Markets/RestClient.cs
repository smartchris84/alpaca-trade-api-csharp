﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for Alpaca REST API and Polygon REST API endpoints.
    /// </summary>
    public sealed partial class RestClient
    {
        private readonly HttpClient _alpacaHttpClient = new HttpClient();

        private readonly HttpClient _polygonHttpClient = new HttpClient();

        private readonly String _polygonApiKey;

        /// <summary>
        /// Creates new instance of <see cref="RestClient"/> object.
        /// </summary>
        /// <param name="keyId">Application key identifier.</param>
        /// <param name="secretKey">Application secret key.</param>
        /// <param name="restApi">REST API endpoint URL.</param>
        public RestClient(
            String keyId,
            String secretKey,
            Uri restApi)
        {
            _alpacaHttpClient.DefaultRequestHeaders.Add(
                "APCA-API-KEY-ID", keyId);
            _alpacaHttpClient.DefaultRequestHeaders.Add(
                "APCA-API-SECRET-KEY", secretKey);
            _alpacaHttpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _alpacaHttpClient.BaseAddress = restApi;

            // TODO: olegra - provide correct key and probably endpoint here
            _polygonApiKey = keyId;
            _polygonHttpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _polygonHttpClient.BaseAddress = 
                new Uri("https://api.polygon.io");
        }

        private async Task<TApi> getSingleObjectAsync<TApi, TJson>(
            HttpClient httpClient,
            String endpointUri)
            where TJson : TApi
        {
            using (var stream = await httpClient.GetStreamAsync(endpointUri))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<TJson>(reader);
            }
        }

        private Task<TApi> getSingleObjectAsync<TApi, TJson>(
            HttpClient httpClient,
            UriBuilder uriBuilder)
            where TJson : TApi
        {
            return getSingleObjectAsync<TApi, TJson>(httpClient, uriBuilder.ToString());
        }

        private async Task<IEnumerable<TApi>> getObjectsListAsync<TApi, TJson>(
            HttpClient httpClient,
            String endpointUri)
            where TJson : TApi
        {
            return (IEnumerable<TApi>) await
                getSingleObjectAsync<IEnumerable<TJson>, List<TJson>>(httpClient, endpointUri);
        }

        private async Task<IEnumerable<TApi>> getObjectsListAsync<TApi, TJson>(
            HttpClient httpClient,
            UriBuilder uriBuilder)
            where TJson : TApi
        {
            return (IEnumerable<TApi>) await
                getSingleObjectAsync<IEnumerable<TJson>, List<TJson>>(httpClient, uriBuilder);
        }
    }
}
