using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.TemplateModels
{
    public class SubscriptionsTemplateResource : TemplateResource
    {
        public SubscriptionsTemplateProperties properties { get; set; }
    }

    public class SubscriptionsTemplateProperties
    {
        public string ownerId { get; set; }
        public string scope { get; set; }
        public string displayName { get; set; }
        public string primaryKey { get; set; }
        public string secondaryKey { get; set; }
        public string state { get; set; }
        public bool? allowTracing { get; set; }
    }
}