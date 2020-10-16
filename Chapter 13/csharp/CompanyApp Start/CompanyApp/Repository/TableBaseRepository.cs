using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;

namespace CompanyApp.Repository
{
    public class TableBaseRepository<T>
       where T : TableEntity, new()
    {
        private readonly string _defaultPartitionKey;

        public TableBaseRepository(
            IConfiguration configuration,
            string tableName,
            string defaultPartitionKey)
        {
            var storageAccountConnectionString = configuration["StorageAccountConnectionString"];
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            Table = tableClient.GetTableReference(tableName);
            Table.CreateIfNotExists();
            _defaultPartitionKey = defaultPartitionKey;
        }

        public CloudTable Table { get; }


        public async Task CreateOrUpdateAsync(T entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);

            await this.Table.ExecuteAsync(operation);
        }

        public async Task InsertOrMergeAsync(T entity)
        {
            var operation = TableOperation.InsertOrMerge(entity);

            await this.Table.ExecuteAsync(operation);
        }

        public async Task DeleteAsync(T entity)
        {
            var operation = TableOperation.Delete(entity);

            await this.Table.ExecuteAsync(operation);
        }

        public async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<T>(partitionKey, rowKey);

            var result = await this.Table.ExecuteAsync(operation);

            return result.Result as T;
        }
        private async Task<IList<T>> ExecuteQueryAsync(
            TableQuery<T> query,
            int? count = null,
            CancellationToken ct = default)
        {
            query.TakeCount = count;

            try
            {
                var result = new List<T>();
                TableContinuationToken token = null;

                do
                {
                    TableQuerySegment<T> seg = await Table.ExecuteQuerySegmentedAsync(query, token, ct);
                    token = seg.ContinuationToken;
                    result.AddRange(seg);
                }
                while (token != null
                       && !ct.IsCancellationRequested
                       && (count == null || result.Count < count.Value));

                return result;
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Get entities from the table storage in a partition with a filter.
        /// </summary>
        /// <param name="filter">Filter to the result.</param>
        /// <param name="partition">Partition key value.</param>
        /// <returns>All data entities.</returns>
        public async Task<IEnumerable<T>> GetWithFilterAsync(string filter, string partition = null)
        {
            var partitionKeyFilter = this.GetPartitionKeyFilter(partition);

            var combinedFilter = this.CombineFilters(filter, partitionKeyFilter);

            var query = new TableQuery<T>().Where(combinedFilter);

            var entities = await this.ExecuteQueryAsync(query);

            return entities;
        }

        /// <summary>
        /// Get all data entities from the table storage in a partition.
        /// </summary>
        /// <param name="partition">Partition key value.</param>
        /// <param name="count">The max number of desired entities.</param>
        /// <returns>All data entities.</returns>
        public async Task<IEnumerable<T>> GetAllAsync(string partition = null, int? count = null)
        {
            var partitionKeyFilter = this.GetPartitionKeyFilter(partition);

            var query = new TableQuery<T>().Where(partitionKeyFilter);

            var entities = await this.ExecuteQueryAsync(query, count);

            return entities;
        }

        private string CombineFilters(string filter1, string filter2)
        {
            if (string.IsNullOrWhiteSpace(filter1) && string.IsNullOrWhiteSpace(filter2))
            {
                return string.Empty;
            }
            else if (string.IsNullOrWhiteSpace(filter1))
            {
                return filter2;
            }
            else if (string.IsNullOrWhiteSpace(filter2))
            {
                return filter1;
            }

            return TableQuery.CombineFilters(filter1, TableOperators.And, filter2);
        }

        private string GetPartitionKeyFilter(string partition)
        {
            var filter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal,
                string.IsNullOrWhiteSpace(partition) ? this._defaultPartitionKey : partition);
            return filter;
        }


    }
}
