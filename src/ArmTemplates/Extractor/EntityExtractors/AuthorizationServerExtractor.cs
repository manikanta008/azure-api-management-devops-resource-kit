﻿using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.EntityExtractors.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.TemplateModels;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Constants;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.EntityExtractors
{
    public class AuthorizationServerExtractor : EntityExtractorBase, IAuthorizationServerExtractor
    {
        public async Task<string> GetAuthorizationServersAsync(string ApiManagementName, string ResourceGroupName)
        {
            (string azToken, string azSubId) = await this.Auth.GetAccessToken();

            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/authorizationServers?api-version={4}",
               BaseUrl, azSubId, ResourceGroupName, ApiManagementName, GlobalConstants.APIVersion);

            return await this.CallApiManagementAsync(azToken, requestUrl);
        }

        public async Task<string> GetAuthorizationServerDetailsAsync(string ApiManagementName, string ResourceGroupName, string authorizationServerName)
        {
            (string azToken, string azSubId) = await this.Auth.GetAccessToken();

            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/authorizationServers/{4}?api-version={5}",
               BaseUrl, azSubId, ResourceGroupName, ApiManagementName, authorizationServerName, GlobalConstants.APIVersion);

            return await this.CallApiManagementAsync(azToken, requestUrl);
        }

        public async Task<Template> GenerateAuthorizationServersARMTemplateAsync(string apimname, string resourceGroup, string singleApiName, List<TemplateResource> apiTemplateResources)
        {
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("Extracting authorization servers from service");
            Template armTemplate = this.GenerateEmptyPropertyTemplateWithParameters();

            List<TemplateResource> templateResources = new List<TemplateResource>();

            // isolate api resources in the case of a single api extraction, as they may reference authorization servers
            var apiResources = apiTemplateResources.Where(resource => resource.type == ResourceTypeConstants.API);

            // pull all authorization servers for service
            string authorizationServers = await this.GetAuthorizationServersAsync(apimname, resourceGroup);
            JObject oAuthorizationServers = JObject.Parse(authorizationServers);

            foreach (var item in oAuthorizationServers["value"])
            {
                string authorizationServerName = ((JValue)item["name"]).Value.ToString();
                string authorizationServer = await this.GetAuthorizationServerDetailsAsync(apimname, resourceGroup, authorizationServerName);

                // convert returned authorization server to template resource class
                AuthorizationServerTemplateResource authorizationServerTemplateResource = JsonConvert.DeserializeObject<AuthorizationServerTemplateResource>(authorizationServer);
                authorizationServerTemplateResource.name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{authorizationServerName}')]";
                authorizationServerTemplateResource.apiVersion = GlobalConstants.APIVersion;

                // only extract the authorization server if this is a full extraction, or in the case of a single api, if it is referenced by one of the api's authentication settings
                bool isReferencedByAPI = false;
                foreach (APITemplateResource apiResource in apiResources)
                {
                    if (apiResource.properties.authenticationSettings != null &&
                        apiResource.properties.authenticationSettings.oAuth2 != null &&
                        apiResource.properties.authenticationSettings.oAuth2.authorizationServerId != null &&
                        apiResource.properties.authenticationSettings.oAuth2.authorizationServerId.Contains(authorizationServerName))
                    {
                        isReferencedByAPI = true;
                    }
                }
                if (singleApiName == null || isReferencedByAPI)
                {
                    Console.WriteLine("'{0}' Authorization server found", authorizationServerName);
                    templateResources.Add(authorizationServerTemplateResource);
                }
            }

            armTemplate.resources = templateResources.ToArray();
            return armTemplate;
        }
    }
}
