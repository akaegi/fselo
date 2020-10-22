using System;
using System.Threading;
using System.Threading.Tasks;
using Eveneum;
using Eveneum.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace FsElo.WebApp
{
    public static class EveneumProvider
    {
        public static void ProvideEveneum(this IServiceCollection services)
        {
            services.AddSingleton<EveneumInitializer>();
            services.AddSingleton<IEventStore>(sp => sp.GetService<EveneumInitializer>().EventStore);
            services.AddSingleton<IReadStream>(sp => sp.GetService<EveneumInitializer>().EventStore);
            services.AddSingleton<IWriteToStream>(sp => sp.GetService<EveneumInitializer>().EventStore);
            services.AddSingleton<EveneumDocumentSerializer>(sp => 
                sp.GetService<EveneumInitializer>().EventStore.Serializer);
        }
    }

    public class EveneumInitializer: IDisposable
    {
        private readonly CosmosClient _cosmosClient;
        private readonly ManualResetEvent _ready = new ManualResetEvent(false);
        private EventStore _eventStore;
        
        public EveneumInitializer(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }


        public async Task DoAsync()
        {
            var dbr = await _cosmosClient.CreateDatabaseIfNotExistsAsync("FsElo");
            await dbr
                .Database
                .CreateContainerIfNotExistsAsync("Events", "/StreamId");

            _eventStore = new EventStore(_cosmosClient, "FsElo", "Events");
            await _eventStore.Initialize();
            _ready.Set();
        }

        public EventStore EventStore
        {
            get
            {
                _ready.WaitOne();
                return _eventStore;
            }
        }

        public void Dispose()
        {
            _ready.Dispose();
        }
    }
}