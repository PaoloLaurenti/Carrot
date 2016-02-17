using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Carrot.Configuration;
using Carrot.Fallback;
using Carrot.Messages;
using Carrot.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Yoox.Backend.Messages.Article;
using JsonSerializer = Carrot.Serialization.JsonSerializer;

namespace Carrot.BasicSample
{
    public class Program
    {
        private static void Main()
        {
            const String routingKey = "";
            const String endpointUrl = "amqp://guest:guest@localhost:5672/DPCataloguingDev";
            var messageBindingResolver = new MessageBindingResolver(typeof(Foo).Assembly);
            var resolver = new CustomResolver(messageBindingResolver, typeof(ICurrentArticleVersionChanged).Assembly);

            var broker = Broker.New(_ =>
            {
                _.Endpoint(new Uri(endpointUrl, UriKind.Absolute));
                _.ConfigureSerialization(serializationConfiguration =>
                {
                    serializationConfiguration.Map(x => x.MediaType == "application/vnd.masstransit+json", new MassTransitJsonSerializer());
                });
                _.ResolveMessageTypeBy(resolver);
            });

            var exchange = broker.DeclareDurableFanoutExchange("DP.ShippingRestrictions.Engine");
            var queue = broker.DeclareDurableQueue("DP.ShippingRestrictions.Engine");
            broker.DeclareExchangeBinding(exchange, queue, routingKey);
            broker.SubscribeByAtLeastOnce(queue, _ =>
            {
                _.FallbackBy((c, a) => DeadLetterStrategy.New(c, a, x => $"{x}_error2"));
                _.Consumes(new FooConsumer1());
                _.Consumes(new FooConsumerArticle());
            });
            //broker.SubscribeByAtLeastOnce(queue, _ =>
            //{
            //    _.FallbackBy((c, a) => DeadLetterStrategy.New(c, a, x => $"{x}-Error"));
            //    _.Consumes(new FooConsumer2());
            //});
            var connection = broker.Connect();

            //for (var i = 0; i < 5; i++)
            //{
            //    var message = new OutboundMessage<Foo>(new Foo { Bar = i });
            //    connection.PublishAsync(message, exchange, routingKey);
            //}

            //var message = new OutboundMessage<Foo>(new Foo { Bar = 42 });
            //connection.PublishAsync(message, exchange, routingKey);

            Console.ReadLine();
            connection.Dispose();
        }
    }

    internal class MassTransitJsonSerializer : ISerializer
    {
        private readonly JsonSerializer _jsonSerializer;

        public MassTransitJsonSerializer()
        {
            _jsonSerializer = new JsonSerializer();
        }

        private class Envelope
        {
            public Object Message { get; set; }
        }

        public object Deserialize(byte[] body, Type type, Encoding encoding = null)
        {
            var envelope = _jsonSerializer.Deserialize(body, typeof(Envelope), encoding) as Envelope;
            if (envelope == null)
                return null;

            var jObject = envelope.Message as JObject;
            if (jObject == null)
                return null;

            return jObject.ToObject(type);
        }

        public string Serialize(object obj)
        {
            return _jsonSerializer.Serialize(obj);
        }
    }

    internal class CustomResolver : IMessageTypeResolver
    {
        private readonly IMessageTypeResolver _messageTypeResolver;
        private readonly Assembly _assembly;

        public CustomResolver(IMessageTypeResolver messageTypeResolver, Assembly assembly)
        {
            _messageTypeResolver = messageTypeResolver;
            _assembly = assembly;
        }

        public MessageBinding Resolve(ConsumedMessageContext context)
        {
            if (context.ContentType.Equals("application/vnd.masstransit+json", StringComparison.InvariantCultureIgnoreCase))
            {
                var massTransitType = Assembly
                                        .GetExecutingAssembly()
                                        .GetTypes()
                                        .SingleOrDefault(t => _assembly.GetType(context.Source.Replace(':', '.'))
                                        .IsAssignableFrom(t));
                return new MessageBinding(context.Source, massTransitType);
            }

            return _messageTypeResolver.Resolve(context);
        }

        public MessageBinding Resolve<TMessage>() where TMessage : class
        {
            return _messageTypeResolver.Resolve<TMessage>();
        }
    }
}