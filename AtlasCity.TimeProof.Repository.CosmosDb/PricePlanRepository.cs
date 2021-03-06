﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AtlasCity.TimeProof.Abstractions.DAO;
using AtlasCity.TimeProof.Abstractions.Repository;
using Dawn;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace AtlasCity.TimeProof.Repository.CosmosDb
{
    public class PricePlanRepository : CosmosDbBaseRepository, IPricePlanRepository
    {
        private const string DatabaseId = "DocumentStamp";
        private const string CollectionId = "PricePlans";

        private readonly Uri _documentCollectionUri;

        public PricePlanRepository(string endpointUrl, string authorizationKey) :
          base(endpointUrl, authorizationKey)
        {
            _documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId);
        }

        public async Task<IEnumerable<PricePlanDao>> GetPricePlans(CancellationToken cancellationToken)
        {
            // TODO: Sudhir Only return active price plan

            var option = new FeedOptions { EnableCrossPartitionQuery = true };
            var response = Client.CreateDocumentQuery<PricePlanDao>(_documentCollectionUri, option).AsEnumerable();
            return response;
        }

        public async Task<PricePlanDao> GetPricePlanById(string pricePlanId, CancellationToken cancellationToken)
        {
            Guard.Argument(pricePlanId, nameof(pricePlanId)).NotNull().NotEmpty().NotWhiteSpace();

            var option = new FeedOptions { EnableCrossPartitionQuery = true };
            var response = Client.CreateDocumentQuery<PricePlanDao>(_documentCollectionUri, option).Where(s => s.Id.ToLower() == pricePlanId.ToLower()).AsEnumerable().FirstOrDefault();
            return response;
        }

        public async Task<PricePlanDao> GetPricePlanByTitle(string pricePlanTitle, CancellationToken cancellationToken)
        {
            Guard.Argument(pricePlanTitle, nameof(pricePlanTitle)).NotNull().NotEmpty().NotWhiteSpace();

            var option = new FeedOptions { EnableCrossPartitionQuery = true };
            var response = Client.CreateDocumentQuery<PricePlanDao>(_documentCollectionUri, option).Where(s => s.Title.ToLower() == pricePlanTitle.ToLower()).AsEnumerable().FirstOrDefault();
            return response;
        }

        public async Task<PricePlanDao> AddPricePlans(PricePlanDao pricePlan, CancellationToken cancellationToken)
        {
            Guard.Argument(pricePlan, nameof(pricePlan)).NotNull();
            Guard.Argument(pricePlan.Title, nameof(pricePlan.Title)).NotNull().NotEmpty().NotWhiteSpace();

            var response = await Client.CreateDocumentAsync(_documentCollectionUri, pricePlan, new RequestOptions(), false, cancellationToken);
            return JsonConvert.DeserializeObject<PricePlanDao>(response.Resource.ToString());
        }
    }
}