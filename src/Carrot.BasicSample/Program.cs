using System;
using System.Text.RegularExpressions;
using Carrot.Configuration;
using Carrot.Fallback;
using Carrot.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using JsonSerializer = Carrot.Serialization.JsonSerializer;

namespace Carrot.BasicSample
{
    public class Program
    {
        private static void Main()
        {
            const String routingKey = "routing_key";
            const String endpointUrl = "amqp://guest:guest@localhost:5672/";
            IMessageTypeResolver resolver = new MessageBindingResolver(typeof(Foo).Assembly);

            var broker = Broker.New(_ =>
            {
                _.Endpoint(new Uri(endpointUrl, UriKind.Absolute));
                _.ConfigureSerialization(sc => sc.ConfigureSerializer<JsonSerializer>("application/json", x =>
                {
                    x.Settings.DefaultValueHandling = DefaultValueHandling.Include;
                    x.Settings.ContractResolver = new JsonLowerCaseUnderscoreContractResolver();
                }));
                _.ResolveMessageTypeBy(resolver);
            });

            var exchange = broker.DeclareDirectExchange("source_exchange");
            var queue = broker.DeclareQueue("my_test_queue");
            broker.DeclareExchangeBinding(exchange, queue, routingKey);
            //broker.SubscribeByAtLeastOnce(queue, _ =>
            //{
            //    _.FallbackBy((c, a) => DeadLetterStrategy.New(c, a, x => $"{x}-Error"));
            //    _.Consumes(new FooConsumer1());
            //});
            //broker.SubscribeByAtLeastOnce(queue, _ =>
            //{
            //    _.FallbackBy((c, a) => DeadLetterStrategy.New(c, a, x => $"{x}-Error"));
            //    _.Consumes(new FooConsumer2());
            //});
            var connection = broker.Connect();

            for (var i = 0; i < 5; i++)
            {
                var message = new OutboundMessage<Foo>(new Foo { Bar = i });
                connection.PublishAsync(message, exchange, routingKey);
            }

            Console.ReadLine();
            connection.Dispose();
        }
    }

    public class JsonLowerCaseUnderscoreContractResolver : DefaultContractResolver
    {
        private readonly Regex _regex = new Regex("(?!(^[A-Z]))([A-Z])");

        protected override string ResolvePropertyName(string propertyName)
        {
            return _regex.Replace(propertyName, "_$2").ToLower();
        }
    }
}