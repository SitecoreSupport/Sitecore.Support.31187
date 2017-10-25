
namespace Sitecore.Support.Commerce.UX.CustomerOrderManager.Repositories
{
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Linq;
    using Sitecore.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Sitecore.Commerce.UX.CustomerOrderManager;
    using Sitecore.Commerce.UX.CustomerOrderManager.Repositories;

    /// <summary>
    /// A class that represents a repository for search.
    /// </summary>
    public class SearchRepository : ISearchRepository
    {
        private const string OrdersIndexName = "commerce_orders_index";
        private const string CustomersIndexName = "commerce_userprofiles_index_master";

        private const string OrderPlacedDate = "orderplaceddate";

        /// <summary>
        /// Gets the search results for the given entity type and search term. The optional parentId
        /// parameter restricts the search to the child elements specific entity instance.
        /// </summary>
        /// <param name="itemType">The entity type</param>
        /// <param name="searchTerm">The search term</param>
        /// <param name="parentId">The parent id</param>
        /// <param name="sortDirection">The sort direction</param>
        /// <param name="sortProperty">The sort property</param>
        /// <param name="pageIndex">The page index</param>
        /// <param name="pageSize">The page size</param>
        /// <param name="environment">The environment.</param>
        /// <param name="totalItemCount">The total item count</param>
        /// <param name="requestedProperties">The requested properties</param>
        /// <returns>
        /// The search results
        /// </returns>
        /// <exception cref="System.InvalidOperationException">itemType not recognized</exception>
        public List<object> GetSearchResults(string itemType, string searchTerm, string parentId, string sortDirection, string sortProperty, int pageIndex, int pageSize, string environment, out int totalItemCount, List<string> requestedProperties)
        {
            Assert.IsNotNullOrEmpty(itemType, "The parameter itemType cannot be empty.");

            if (searchTerm != null)
            {
                searchTerm = searchTerm.Trim();
            }

            totalItemCount = 0;

            if (itemType.Equals("customer", StringComparison.OrdinalIgnoreCase))
            {
                return this.GetCustomerSearchResults(
                    searchTerm,
                    sortDirection,
                    sortProperty,
                    pageIndex,
                    pageSize,
                    environment,
                    out totalItemCount,
                    requestedProperties);
            }

            if (itemType.Equals("order", StringComparison.OrdinalIgnoreCase))
            {
                return this.GetOrderSearchResults(
                    searchTerm,
                    sortDirection,
                    sortProperty,
                    pageIndex,
                    pageSize,
                    environment,
                    out totalItemCount,
                    requestedProperties);
            }

            throw new InvalidOperationException("itemType not recognized");
        }

        /// <summary>
        /// Gets the order search results.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="sortDirection">The sort direction.</param>
        /// <param name="sortProperty">The sort property.</param>
        /// <param name="pageIndex">Index of the page.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="artifactStoreId">The environment.</param>
        /// <param name="totalItemCount">The total item count.</param>
        /// <param name="requestedProperties">The requested properties.</param>
        /// <returns>A list of orders</returns>
        private List<object> GetOrderSearchResults(string searchTerm, string sortDirection, string sortProperty, int pageIndex, int pageSize, string artifactStoreId, out int totalItemCount, List<string> requestedProperties)
        {
            ISearchIndex ordersIndex = ContentSearchManager.GetIndex(OrdersIndexName);

            using (var context = ordersIndex.CreateSearchContext())
            {
                var searchQuery = context.GetQueryable<OrderSearchResultItem>();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchQuery = searchQuery.Where(
                        r => (r.OrderConfirmationId.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) || r.Email.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
                        && r.ArtifactStoreId.Equals(artifactStoreId, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    searchQuery = searchQuery.Where(r => r.ArtifactStoreId.Equals(artifactStoreId, StringComparison.OrdinalIgnoreCase));
                }

                totalItemCount = searchQuery.Count();

                var isPagingSpecified = pageSize > 0 && pageIndex >= 0;

                searchQuery = this.ApplySorting(searchQuery, sortProperty, sortDirection);

                if (isPagingSpecified)
                {
                    searchQuery = searchQuery.Skip(pageIndex * pageSize).Take(pageSize);
                }

                var searchResults = searchQuery.GetResults();

                var resultsItems = searchResults.Select(r => this.CreateOrderResult(r.Document, requestedProperties));

                return resultsItems.ToList<object>();
            }
        }

        private IQueryable<OrderSearchResultItem> ApplySorting(IQueryable<OrderSearchResultItem> searchQuery, string sortProperty, string sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortProperty))
            {
                return searchQuery;
            }

            if (sortProperty == OrderPlacedDate)
            {
                return sortDirection == "Asc"
                    ? searchQuery.OrderBy(order => order.OrderDate)
                    : searchQuery.OrderByDescending(order => order.OrderDate);
            }

            return sortDirection == "Asc" ? 
                searchQuery.OrderBy(order => order[sortProperty]) : 
                searchQuery.OrderByDescending(order => order[sortProperty]);
        }

        /// <summary>
        /// Gets the customer search results.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="sortDirection">The sort direction.</param>
        /// <param name="sortProperty">The sort property.</param>
        /// <param name="pageIndex">Index of the page.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="totalItemCount">The total item count.</param>
        /// <param name="requestedProperties">The requested properties.</param>
        /// <returns>A list of Customers</returns>
        private List<object> GetCustomerSearchResults(string searchTerm, string sortDirection, string sortProperty, int pageIndex, int pageSize, string environment, out int totalItemCount, List<string> requestedProperties)
        {
            ISearchIndex customersIndex = ContentSearchManager.GetIndex(CustomersIndexName);

            using (var context = customersIndex.CreateSearchContext())
            {
                var searchQuery = context.GetQueryable<CustomerSearchResultItem>();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    if (searchTerm.Contains("*"))
                    {
                        if (searchTerm.EndsWith("*", StringComparison.OrdinalIgnoreCase))
                        {
                            searchQuery = searchQuery.Where(
                                r => (r.UserId.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                r.Email.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                r.FirstName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                r.LastName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                r.Content.Contains(searchTerm)));
                        }
                        else if (searchTerm.StartsWith("*", StringComparison.OrdinalIgnoreCase))
                        {
                            searchQuery = searchQuery.Where(
                                r => (r.UserId.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                r.Email.EndsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                r.FirstName.EndsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                r.LastName.EndsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                r.Content.Contains(searchTerm)));
                        }
                    }
                    else
                    {
                        searchQuery = searchQuery.Where(
                            r => (r.UserId.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            r.Email.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            r.FirstName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            r.LastName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            r.Content.Equals(searchTerm)));
                    }
                }

                totalItemCount = searchQuery.Count();

                var isSortingSpecified = !string.IsNullOrWhiteSpace(sortProperty);
                var isPagingSpecified = pageSize > 0 && pageIndex >= 0;

                if (isSortingSpecified)
                {
                    if (sortDirection == "Asc")
                    {
                        searchQuery = searchQuery.OrderBy(customer => customer[sortProperty]);
                    }
                    else
                    {
                        searchQuery = searchQuery.OrderByDescending(customer => customer[sortProperty]);
                    }
                }

                if (isPagingSpecified)
                {
                    searchQuery = searchQuery.Skip(pageIndex * pageSize).Take(pageSize);
                }

                var searchResults = searchQuery.GetResults();

                var resultsItems = searchResults.Select(r => this.CreateCustomerResult(r.Document, requestedProperties));

                return resultsItems.ToList<object>();
            }
        }

        /// <summary>
        /// Creates the customer result.
        /// </summary>
        /// <param name="resultItem">The result item.</param>
        /// <param name="requestedFields">The requested fields.</param>
        /// <returns>A list of Customer results</returns>
        private object CreateCustomerResult(CustomerSearchResultItem resultItem, List<string> requestedFields)
        {
            // Always include UserID, LastName, FirstName and email
            var result = new Dictionary<string, object>();
            result.Add("Id", resultItem.UserId);
            result.Add("first_name", resultItem.FirstName);
            result.Add("last_name", resultItem.LastName);
            result.Add("email_address", resultItem.Email);
            if (!resultItem.ExternalId.Contains("CommerceUsers"))
            {
                result.Add("ItemId", resultItem.ExternalId);
            }
            else
            {
                result.Add("ItemId", resultItem.UserId);
            }

            result.Add("Template", "Customer");

            // TODO: Get the last order date from the customer's orders
            result.Add("LastOrderDate", DateUtil.ToIsoDate(DateTime.Now));
            result.Add("customertargeturl", "/sitecore/client/Applications/CustomerOrderManager/Customer?target=" + resultItem.UserId);

            requestedFields.ForEach(f =>
            {
                if (!result.ContainsKey(f.ToLower(CultureInfo.InvariantCulture)))
                {
                    var fieldValue = resultItem.Fields[f];

                    if (fieldValue.GetType() == typeof(DateTime))
                    {
                        result.Add(f, DateUtil.ToIsoDate((DateTime)fieldValue));
                    }
                    else
                    {
                        result.Add(f, resultItem.Fields[f]);
                    }
                }
            });

            return result;
        }

        /// <summary>
        /// Creates the order result.
        /// </summary>
        /// <param name="resultItem">The result item.</param>
        /// <param name="requestedFields">The requested fields.</param>
        /// <returns>A list of Order results</returns>
        private object CreateOrderResult(OrderSearchResultItem resultItem, List<string> requestedFields)
        {
            // Always include order id, order confirmation, order date, and orderTargetUrl
            var result = new Dictionary<string, object>();

            result.Add("orderid", resultItem.OrderId);
            result.Add("orderconfirmationid", resultItem.OrderConfirmationId);
            result.Add("ordertargeturl", "/sitecore/client/Applications/CustomerOrderManager/Order?target=" + resultItem.OrderId);
            result.Add(OrderPlacedDate, DateUtil.ToIsoDate(resultItem.OrderDate));

            requestedFields.ForEach(f =>
            {
                if (!result.ContainsKey(f.ToLower(CultureInfo.InvariantCulture)))
                {
                    var fieldValue = resultItem.Fields[f];

                    if (fieldValue.GetType() == typeof(DateTime))
                    {
                        result.Add(f, DateUtil.ToIsoDate((DateTime)fieldValue));
                    }
                    else
                    {
                        result.Add(f, resultItem.Fields[f]);
                    }
                }
            });

            return result;
        }
    }
}