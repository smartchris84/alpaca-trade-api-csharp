﻿using System;
using System.Collections.Generic;
using System.IO;
using NATS.Client;
using Newtonsoft.Json;

namespace Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for Polygon streaming API.
    /// </summary>
    public sealed class NatsClient : IDisposable
    {
        private readonly IDictionary<String, IAsyncSubscription> _subscriptions =
            new Dictionary<String, IAsyncSubscription>(StringComparer.Ordinal);

        private readonly Options _options;

        private IConnection _connection;

        /// <summary>
        /// Creates new instance of <see cref="NatsClient"/> object.
        /// </summary>
        /// <param name="keyId">Application key identifier.</param>
        public NatsClient(
            String keyId)
        {
            _options = ConnectionFactory.GetDefaultOptions();
            _options.MaxReconnect = 3;
            _options.Servers = new[]
            {
                $"nats://{keyId}@nats1.polygon.io:31101",
                $"nats://{keyId}@nats2.polygon.io:31102",
                $"nats://{keyId}@nats3.polygon.io:31103"
            };

            _options.AsyncErrorEventHandler += (sender, args) => OnError?.Invoke(args.Error);
        }

        /// <summary>
        /// Occurrs when new trade received from stream.
        /// </summary>
        public event Action<IStreamTrade> TradeReceived;

        /// <summary>
        /// Occurrs when new quote received from stream.
        /// </summary>
        public event Action<IStreamQuote> QuoteReceived;

        /// <summary>
        /// Occurrs when new bar received from stream.
        /// </summary>
        public event Action<IStreamBar> BarReceived;

        /// <summary>
        /// Occurrs when any error happened in stream.
        /// </summary>
        public event Action<String> OnError;

        /// <summary>
        /// Opens connection to Polygon streaming API.
        /// </summary>
        public void Open()
        {
            _connection = new ConnectionFactory()
                .CreateConnection(_options);
        }

        /// <summary>
        /// Subscribes for the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeTrade(
            String symbol)
        {
            subscribe($"T.{symbol}", handleTradeMessage);
        }

        /// <summary>
        /// Subscribes for the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeQuote(
            String symbol)
        {
            subscribe($"Q.{symbol}", handleQuoteMessage);
        }

        /// <summary>
        /// Subscribes for the second bar updates via <see cref="BarReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeSecondBar(
            String symbol)
        {
            subscribe($"A.{symbol}", handleBarMessage);
        }

        /// <summary>
        /// Subscribes for the minute bar updates via <see cref="BarReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeMinuteBar(
            String symbol)
        {
            subscribe($"AM.{symbol}", handleBarMessage);
        }

        /// <summary>
        /// Unsubscribes from the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeTrade(
            String symbol)
        {
            unsubscribe($"T.{symbol}");
        }

        /// <summary>
        /// Unsubscribes from the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeQuote(
            String symbol)
        {
            unsubscribe($"Q.{symbol}");
        }

        /// <summary>
        /// Unsubscribes from the second bar updates via <see cref="BarReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeSecondBar(
            String symbol)
        {
            unsubscribe($"A.{symbol}");
        }

        /// <summary>
        /// Unsubscribes from the minute bar updates via <see cref="BarReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeMinuteBar(
            String symbol)
        {
            unsubscribe($"AM.{symbol}");
        }

        /// <summary>
        /// Closes connection to Polygon streaming API.
        /// </summary>
        public void Close()
        {
            foreach (var subscription in _subscriptions.Values)
            {
                subscription?.Unsubscribe();
            }

            _connection?.Close();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var subscription in _subscriptions.Values)
            {
                subscription?.Dispose();
            }

            _connection?.Dispose();
        }

        private void subscribe(
            String topic,
            EventHandler<MsgHandlerEventArgs> handler)
        {
            var subscription = _connection?.SubscribeAsync(topic, handler);

            if (subscription != null)
            {
                _subscriptions[topic] = subscription;
            }
        }

        private void unsubscribe(
            String topic)
        {
            if (_subscriptions.TryGetValue(
                topic, out var subscription))
            {
                subscription?.Unsubscribe();
                subscription?.Dispose();
            }
        }

        private void handleTradeMessage(
            Object sender,
            MsgHandlerEventArgs eventArgs)
        {
            var message = deserializeBytes<JsonStreamTrade>(
                eventArgs.Message.Data);
            TradeReceived?.Invoke(message);
        }

        private void handleQuoteMessage(
            Object sender,
            MsgHandlerEventArgs eventArgs)
        {
            var message = deserializeBytes<JsonStreamQuote>(
                eventArgs.Message.Data);
            QuoteReceived?.Invoke(message);
        }

        private void handleBarMessage(
            Object sender,
            MsgHandlerEventArgs eventArgs)
        {
            var message = deserializeBytes<JsonStreamBar>(
                eventArgs.Message.Data);
            BarReceived?.Invoke(message);
        }

        private static T deserializeBytes<T>(
            Byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var textReader = new StreamReader(stream))
            using (var jsonreader = new JsonTextReader(textReader))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(jsonreader);
            }
        }
    }
}
