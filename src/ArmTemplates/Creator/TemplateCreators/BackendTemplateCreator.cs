﻿using System.Collections.Generic;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Constants;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.TemplateModels;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Creator.Models;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Creator.TemplateCreators
{
    public class BackendTemplateCreator : TemplateCreator
    {
        public Template CreateBackendTemplate(CreatorConfig creatorConfig)
        {
            // create empty template
            Template backendTemplate = this.CreateEmptyTemplate();

            // add parameters
            backendTemplate.parameters = new Dictionary<string, TemplateParameterProperties>
            {
                { ParameterNames.ApimServiceName, new TemplateParameterProperties(){ type = "string" } }
            };

            List<TemplateResource> resources = new List<TemplateResource>();
            foreach (BackendTemplateProperties backendTemplatePropeties in creatorConfig.backends)
            {
                // create backend resource with properties
                BackendTemplateResource backendTemplateResource = new BackendTemplateResource()
                {
                    name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{backendTemplatePropeties.title}')]",
                    type = ResourceTypeConstants.Backend,
                    apiVersion = GlobalConstants.APIVersion,
                    properties = backendTemplatePropeties,
                    dependsOn = new string[] { }
                };
                resources.Add(backendTemplateResource);
            }

            backendTemplate.resources = resources.ToArray();
            return backendTemplate;
        }
    }
}
