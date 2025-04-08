namespace KBChat.Api.Models;

public class AzureDevOpsConfig
{
    public string Organization { get; set; }
    public Wiki[] Wikis { get; set; }


}

public class Wiki
{
    public string Project { get; set; }
    public string WikiName { get; set; }
}
