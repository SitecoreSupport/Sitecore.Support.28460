using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Sitecore.Commerce.Connect.CommerceServer.Search.Models;
using Sitecore.Commerce.UX.Merchandising;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Security;
using Sitecore.Data;
using Sitecore.Globalization;

namespace Sitecore.Support.Commerce.UX.Merchandising
{
    public class CustomCommerceVariantController : BusinessController
    {
        private const string commerceProductsIndex = "commerce_products_master_index";
        private Regex digitsRegex = new Regex("^[1-9][0-9]*$");

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            string text = collection["__Display name"];
            string strVariantId;
            ID sitecoreTemplateId;
            ID iD;
            UpdateActionResult updateActionResult = this.ExtractBaseCreateFields(out strVariantId, out sitecoreTemplateId, out iD);
            if (string.IsNullOrWhiteSpace(text))
            {
                if (updateActionResult == null)
                {
                    updateActionResult = new UpdateActionResult
                    {
                        Status = "failed"
                    };
                }
                updateActionResult.Errors.Add(new ValidationError
                {
                    ControlId = "__Display-name",
                    ErrorMessage = Translate.Text("Display Name is a required field.")
                });
            }
            else if (text.Length > 128)
            {
                if (updateActionResult == null)
                {
                    updateActionResult = new UpdateActionResult
                    {
                        Status = "failed"
                    };
                }
                updateActionResult.Errors.Add(new ValidationError
                {
                    ControlId = "__Display-name",
                    ErrorMessage = Translate.Text("Display Name must be 128 characters or fewer.")
                });
            }
            if (updateActionResult != null)
            {
                return base.Json(updateActionResult);
            }
            if (!this.IsVariantIdUniqueAmongVariantsAndProducts(strVariantId))
            {
                UpdateActionResult updateActionResult2 = new UpdateActionResult
                {
                    Status = "failed"
                };
                updateActionResult2.Errors.Add(new ValidationError
                {
                    ErrorMessage = Translate.Text("A variant/product with that ID already exists.")
                });
                return base.Json(updateActionResult2);
            }
            else if (!IsVariantIdInteger(strVariantId))
            {
                UpdateActionResult updateActionResult3 = new UpdateActionResult
                {
                    Status = "failed"
                };
                updateActionResult3.Errors.Add(new ValidationError
                {
                    ErrorMessage = Translate.Text("A variant must be integer.")
                });
                return base.Json(updateActionResult3);
            }
            return this.CreateItem(collection, strVariantId, sitecoreTemplateId, iD);
        }

        private bool IsVariantIdInteger(string name)
        {
            return digitsRegex.IsMatch(name);
        }

        private bool IsVariantIdUniqueAmongVariantsAndProducts(string name)
        {
            ISearchIndex index = ContentSearchManager.GetIndex(CustomCommerceVariantController.commerceProductsIndex);
            string requestedLanguage = base.GetRequestedLanguageName();
            using (IProviderSearchContext providerSearchContext = index.CreateSearchContext(SearchSecurityOptions.Default))
            {
                IQueryable<CommerceBaseCatalogSearchResultItem> source = from it in providerSearchContext.GetQueryable<CommerceBaseCatalogSearchResultItem>()
                                                                         where ((it.Name == name && it.CommerceSearchItemType == "Product") || (it["variantid"] == name))
                                                                         where it.Language == requestedLanguage
                                                                         select it;
                if (source.Any<CommerceBaseCatalogSearchResultItem>())
                {
                    return false;
                }
            }
            return true;
        }
    }
}