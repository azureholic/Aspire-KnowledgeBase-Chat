using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KBChat.AppHost.Settings;

internal class AzureDevOpsSettings
{
    public const string AzureDevOps = "AzureDevOps";
    public string? Organization { get; set; }
    public List<Wiki>? Wikis { get; set; }

}

internal class Wiki
{
    public string? Project { get; set; }
    public string? WikiName { get; set; }
}