﻿using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.TemplateModels
{
    public class ReleaseTemplateResource : APITemplateSubResource
    {
        public ReleaseTemplateProperties properties { get; set; }
    }

    public class ReleaseTemplateProperties
    {
        public string apiId { get; set; }
        public string notes { get; set; }
    }
}