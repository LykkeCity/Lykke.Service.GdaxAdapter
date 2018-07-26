using System;
using System.Reactive.Linq;
using Lykke.Common.ExchangeAdapter.Tools.ObservableWebSocket;

namespace Lykke.Service.GdaxAdapter.Services
{
    public static class ObservableWebsocketExtensions
    {
        private class MessageReceived<T> : IMessageReceived<T>
        {
            public MessageReceived(WebSocketSession session, T content)
            {
                Session = session;
                Content = content;
            }

            public WebSocketSession Session { get; }
            public T Content { get; }
        }

        public static IObservable<ISocketEvent> ApplyBytesReader<T>(
            this IObservable<ISocketEvent> source,
            Func<byte[], T> reader)
        {
            return source
                .Select(x =>
                {
                    if (x is IMessageReceived<byte[]> msg)
                    {
                        return new MessageReceived<T>(msg.Session, reader(msg.Content));
                    }
                    else return x;
                });
        }

        public static IObservable<T> TakeOnlyContent<T>(this IObservable<ISocketEvent> socket)
        {
            return socket
                .Where(x => x is IMessageReceived<T>)
                .Select(x => (x as IMessageReceived<T>).Content);
        }
    }
}
