
namespace Sitecore.Support.Commerce.UX.CustomerOrderManager.Controllers
{
    using Sitecore.Commerce.UX.CustomerOrderManager.Repositories;
    using Sitecore.Diagnostics;
    using Sitecore.Web;
    using System.Collections.Generic;
    using System.Web.Mvc;
    using Sitecore.Commerce.UX.Shared.Controllers;

    /// <summary>
    /// A controller class for search operations.
    /// </summary>
    public class SupportCustomerOrderSearchController : BaseController
    {
        /// <summary>
        /// The repository for this controller.
        /// </summary>
        private ISearchRepository repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerOrderSearchController"/> class.
        /// </summary>
        /// <param name="repository">The repository</param>
        public SupportCustomerOrderSearchController(ISearchRepository repository)
        {
            Assert.IsNotNull(repository, "repository");
            this.repository = repository;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerOrderSearchController"/> class.
        /// </summary>
        public SupportCustomerOrderSearchController()
            : this(new Sitecore.Support.Commerce.UX.CustomerOrderManager.Repositories.SearchRepository())
        {
            // TODO: IOC/DI
        }

        /// <summary>
        /// Gets the repository for this controller.
        /// </summary>
        protected ISearchRepository Repository
        {
            get
            {
                return this.repository;
            }
        }

        /// <summary>
        /// Gets the search results matching the given search term, entity type, and parent id.
        /// </summary>
        /// <param name="itemType">The type of entity.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="parentId">The parent id</param>
        /// <returns>The customer search results matching the search term.</returns>
        public JsonResult GetSearchResults(string itemType, string searchTerm, string parentId = null)
        {
            var sortDirection = this.GetRequestSortDirection();
            var sortProperty = this.GetRequestSortProperty();
            var pageIndex = this.GetRequestPageIndex();
            var pageSize = this.GetRequestPageSize();
            var requestedProperties = this.GetRequestedFields();
            var language = GetRequestedLanguage();
            var currency = GetRequestedCurrency();
            var environment = GetRequestedEnvironment();

            var totalItemCount = 0;
            var results = this.Repository.GetSearchResults(
                itemType,
                searchTerm,
                parentId,
                sortDirection,
                sortProperty,
                pageIndex,
                pageSize,
                environment,
                out totalItemCount,
                requestedProperties);

            var queryResponse = new { Items = results.ToArray(), TotalItemCount = totalItemCount };
            return Json(queryResponse);
        }

        /// <summary>
        /// Gets the requested environment.
        /// </summary>
        /// <returns>The Environment name or an empty string</returns>
        private string GetRequestedEnvironment()
        {
            var headerString = WebUtil.GetFormValue("Headers");
            if (!string.IsNullOrWhiteSpace(headerString))
            {
                var headers = headerString.Split('|');
                foreach (var item in headers)
                {
                    var headerValues = item.Split(':');
                    if (headerValues[0] == "Environment")
                    {
                        return headerValues[1];
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the requested currency.
        /// </summary>
        /// <returns>The currency in use or an empty string</returns>
        private string GetRequestedCurrency()
        {
            var currency = WebUtil.GetFormValue("Currency");
            if (!string.IsNullOrWhiteSpace(currency))
            {
                return currency;
            }

            return string.Empty;
        }

        private string GetRequestedLanguage()
        {
            var language = WebUtil.GetFormValue("Language");
            if (!string.IsNullOrWhiteSpace(language))
            {
                return language;
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the requested fields
        /// </summary>
        /// <returns>A list of strings that represent the requested fields.</returns>
        private List<string> GetRequestedFields()
        {
            var delimitedFields = WebUtil.GetFormValue("fields");
            var requestedFields = new List<string>();

            if (!string.IsNullOrWhiteSpace(delimitedFields))
            {
                var fields = delimitedFields.Split('|');
                foreach (var field in fields)
                {
                    if (!string.IsNullOrWhiteSpace(field))
                    {
                        requestedFields.Add(field);
                    }
                }
            }

            return requestedFields;
        }

        /// <summary>
        /// Gets the requested sort direction.
        /// </summary>
        /// <returns>The sort direction.</returns>
        private string GetRequestSortDirection()
        {
            var sorting = WebUtil.GetFormValue("Sorting");
            if (!string.IsNullOrWhiteSpace(sorting))
            {
                // By SPEAK convention, the sort direction is
                // the first character of the Sorting variable
                var sortDirection = sorting[0];
                if (sortDirection == 'a')
                {
                    return "Asc";
                }

                if (sortDirection == 'd')
                {
                    return "Desc";
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the requested sort property.
        /// </summary>
        /// <returns>The sort property.</returns>
        private string GetRequestSortProperty()
        {
            var sorting = WebUtil.GetFormValue("Sorting");
            if (!string.IsNullOrWhiteSpace(sorting) && sorting.Length > 1)
            {
                // By SPEAK convention, the sort property is
                // prefixed with the sort direction, so ignore the first character
                var sortProperty = sorting.Substring(1);
                return sortProperty;
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the requested page index.
        /// </summary>
        /// <returns>The page index.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Int32.TryParse(System.String,System.Int32@)", Justification = "Out parameter is set to default value in case conversion fails.")]
        private int GetRequestPageIndex()
        {
            int pageIndex = 0;
            var pageIndexString = WebUtil.GetFormValue("PageIndex");
            if (!string.IsNullOrWhiteSpace(pageIndexString))
            {
                var parseSucceeded = int.TryParse(pageIndexString, out pageIndex);
            }

            return pageIndex;
        }

        /// <summary>
        /// Gets the requested page size.
        /// </summary>
        /// <returns>The page size.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Int32.TryParse(System.String,System.Int32@)", Justification = "Out parameter is set to default value in case conversion fails.")]
        private int GetRequestPageSize()
        {
            int pageSize = 0;
            var pageSizeString = WebUtil.GetFormValue("PageSize");
            if (!string.IsNullOrWhiteSpace(pageSizeString))
            {
                int.TryParse(pageSizeString, out pageSize);
            }

            return pageSize;
        }
    }
}