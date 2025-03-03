﻿using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;
using System;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.FileHandlers;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Constants;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.EntityExtractors.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.TemplateModels;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Policy;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.EntityExtractors
{
    public class ProductExtractor : EntityExtractorBase, IProductExtractor
    {
        public async Task<string> GetProductsAsync(string ApiManagementName, string ResourceGroupName)
        {
            (string azToken, string azSubId) = await this.Auth.GetAccessToken();

            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/products?api-version={4}",
               BaseUrl, azSubId, ResourceGroupName, ApiManagementName, GlobalConstants.APIVersion);

            return await this.CallApiManagementAsync(azToken, requestUrl);
        }

        public async Task<string> GetProductDetailsAsync(string ApiManagementName, string ResourceGroupName, string ProductName)
        {
            (string azToken, string azSubId) = await this.Auth.GetAccessToken();

            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/products/{4}?api-version={5}",
               BaseUrl, azSubId, ResourceGroupName, ApiManagementName, ProductName, GlobalConstants.APIVersion);

            return await this.CallApiManagementAsync(azToken, requestUrl);
        }

        public async Task<string> GetProductPolicyAsync(string ApiManagementName, string ResourceGroupName, string ProductName)
        {
            (string azToken, string azSubId) = await this.Auth.GetAccessToken();

            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/products/{4}/policies/policy?api-version={5}&format=rawxml",
               BaseUrl, azSubId, ResourceGroupName, ApiManagementName, ProductName, GlobalConstants.APIVersion);

            return await this.CallApiManagementAsync(azToken, requestUrl);
        }

        public async Task<string> GetProductGroupsAsync(string ApiManagementName, string ResourceGroupName, string ProductName)
        {
            (string azToken, string azSubId) = await this.Auth.GetAccessToken();

            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/products/{4}/groups?api-version={5}",
            BaseUrl, azSubId, ResourceGroupName, ApiManagementName, ProductName, GlobalConstants.APIVersion);

            return await this.CallApiManagementAsync(azToken, requestUrl);
        }

        public async Task<string> GetProductTagsAsync(string ApiManagementName, string ResourceGroupName, string ProductName)
        {
            (string azToken, string azSubId) = await this.Auth.GetAccessToken();

            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/products/{4}/tags?api-version={5}&format=rawxml",
               BaseUrl, azSubId, ResourceGroupName, ApiManagementName, ProductName, GlobalConstants.APIVersion);

            return await this.CallApiManagementAsync(azToken, requestUrl);
        }

        public async Task<Template> GenerateProductsARMTemplateAsync(string apimname, string resourceGroup, string singleApiName, List<TemplateResource> apiTemplateResources, string fileFolder, ExtractorParameters extractorParameters)
        {
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("Extracting products from service");
            Template armTemplate = this.GenerateEmptyPropertyTemplateWithParameters();

            if (extractorParameters.policyXMLBaseUrl != null && extractorParameters.policyXMLSasToken != null)
            {
                TemplateParameterProperties policyTemplateSasTokenParameterProperties = new TemplateParameterProperties()
                {
                    type = "string"
                };
                armTemplate.parameters.Add(ParameterNames.PolicyXMLSasToken, policyTemplateSasTokenParameterProperties);
            }
            if (extractorParameters.policyXMLBaseUrl != null)
            {
                TemplateParameterProperties policyTemplateBaseUrlParameterProperties = new TemplateParameterProperties()
                {
                    type = "string"
                };
                armTemplate.parameters.Add(ParameterNames.PolicyXMLBaseUrl, policyTemplateBaseUrlParameterProperties);
            }

            // isolate product api associations in the case of a single api extraction
            var productAPIResources = apiTemplateResources.Where(resource => resource.type == ResourceTypeConstants.ProductAPI);

            List<TemplateResource> templateResources = new List<TemplateResource>();

            // pull all products for service
            string products = await this.GetProductsAsync(apimname, resourceGroup);
            JObject oProducts = JObject.Parse(products);

            foreach (var item in oProducts["value"])
            {
                string productName = ((JValue)item["name"]).Value.ToString();
                string productDetails = await this.GetProductDetailsAsync(apimname, resourceGroup, productName);

                // convert returned product to template resource class
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                ProductsTemplateResource productsTemplateResource = JsonConvert.DeserializeObject<ProductsTemplateResource>(productDetails, settings);
                productsTemplateResource.name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{productName}')]";
                productsTemplateResource.apiVersion = GlobalConstants.APIVersion;

                string productGroupDetails = await this.GetProductGroupsAsync(apimname, resourceGroup, productName);
                ProductGroupsTemplateResource productGroupsDetails = JsonConvert.DeserializeObject<ProductGroupsTemplateResource>(productGroupDetails, settings);

                // only extract the product if this is a full extraction, or in the case of a single api, if it is found in products associated with the api
                if (singleApiName == null || productAPIResources.SingleOrDefault(p => p.name.Contains($"/{productName}/")) != null)
                {
                    Console.WriteLine("'{0}' Product found", productName);
                    templateResources.Add(productsTemplateResource);

                    // add product policy resource to template
                    try
                    {
                        var productResourceId = new string[] { $"[resourceId('Microsoft.ApiManagement/service/products', parameters('{ParameterNames.ApimServiceName}'), '{productName}')]" };
                        foreach (ProductGroupsValue ProductGroup in productGroupsDetails.value)
                        {
                            ProductGroup.name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{productName}/{ProductGroup.name}')]";
                            ProductGroup.apiVersion = GlobalConstants.APIVersion;
                            ProductGroup.dependsOn = productResourceId;
                            templateResources.Add(ProductGroup);
                        }
                        string productPolicy = await this.GetProductPolicyAsync(apimname, resourceGroup, productName);
                        Console.WriteLine($" - Product policy found for {productName} product");
                        PolicyTemplateResource productPolicyResource = JsonConvert.DeserializeObject<PolicyTemplateResource>(productPolicy);
                        productPolicyResource.name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{productName}/policy')]";
                        productPolicyResource.apiVersion = GlobalConstants.APIVersion;
                        productPolicyResource.scale = null;
                        productPolicyResource.dependsOn = productResourceId;

                        // write policy xml content to file and point to it if policyXMLBaseUrl is provided
                        if (extractorParameters.policyXMLBaseUrl != null)
                        {
                            string policyXMLContent = productPolicyResource.properties.value;
                            string policyFolder = string.Concat(fileFolder, $@"/policies");
                            string productPolicyFileName = $@"/{productName}-productPolicy.xml";
                            FileWriter.CreateFolderIfNotExists(policyFolder);
                            FileWriter.WriteXMLToFile(policyXMLContent, string.Concat(policyFolder, productPolicyFileName));
                            productPolicyResource.properties.format = "rawxml-link";
                            if (extractorParameters.policyXMLSasToken != null)
                            {
                                productPolicyResource.properties.value = $"[concat(parameters('{ParameterNames.PolicyXMLBaseUrl}'), '{productPolicyFileName}', parameters('{ParameterNames.PolicyXMLSasToken}'))]";
                            }
                            else
                            {
                                productPolicyResource.properties.value = $"[concat(parameters('{ParameterNames.PolicyXMLBaseUrl}'), '{productPolicyFileName}')]";
                            }
                        }

                        templateResources.Add(productPolicyResource);
                    }
                    catch (Exception) { }

                    // add tags associated with the product to template 
                    try
                    {
                        // pull tags associated with the product
                        string productTags = await this.GetProductTagsAsync(apimname, resourceGroup, productName);
                        JObject oProductTags = JObject.Parse(productTags);

                        foreach (var tag in oProductTags["value"])
                        {
                            string productTagName = ((JValue)tag["name"]).Value.ToString();
                            Console.WriteLine(" - '{0}' Tag association found for {1} product", productTagName, productName);

                            // convert associations between product and tags to template resource class
                            TagTemplateResource productTagResource = JsonConvert.DeserializeObject<TagTemplateResource>(tag.ToString());
                            productTagResource.name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{productName}/{productTagName}')]";
                            productTagResource.apiVersion = GlobalConstants.APIVersion;
                            productTagResource.scale = null;
                            productTagResource.dependsOn = new string[] { $"[resourceId('Microsoft.ApiManagement/service/products', parameters('{ParameterNames.ApimServiceName}'), '{productName}')]" };
                            templateResources.Add(productTagResource);
                        }
                    }
                    catch (Exception) { }
                }
            }

            armTemplate.resources = templateResources.ToArray();
            return armTemplate;
        }
    }
}
