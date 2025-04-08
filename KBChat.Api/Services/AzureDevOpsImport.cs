using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using KBChat.Api.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KBChat.Api.Services;

public class AzureDevOpsImport(IConfiguration config)
{
    private int docsImported = 0;
    public async Task ImportWiki()
    {
        var managedIdentityClientId = config["AZURE_CLIENT_ID"];
        var azureCredential = new ManagedIdentityCredential(managedIdentityClientId);
        var localCredential = new AzureCliCredential();
        TokenCredential tokenCredential = localCredential;

        if (bool.Parse(config["IS_CLOUD"]))
        {
            tokenCredential = azureCredential;
        }

        BlobServiceClient blobServiceClient = new(
            serviceUri: new Uri(config["AZURE_STORAGE_ENDPOINT"]),
            credential: tokenCredential
        );
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(config["AZURE_STORAGE_CONTAINER"]);

        string recursionLevel = "full"; // none, shallow, full
        string includeContent = "true";
        var token = config["AZURE_DEVOPS_PAT"];
        var devOpsConfigBytes = Convert.FromBase64String(config["AZURE_DEVOPS_WIKIS"]!);
        var devOpsConfigString = Encoding.UTF8.GetString(devOpsConfigBytes);
        var azureDevOps = JsonSerializer.Deserialize<AzureDevOpsConfig>(devOpsConfigString);

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            scheme: "Basic",
            parameter: Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", token)))
        );

        foreach (var wiki in azureDevOps.Wikis)
        { 
            Console.WriteLine($"Importing wiki: {wiki.WikiName}");
            var url = $"https://dev.azure.com/{azureDevOps.Organization}/{wiki.Project}/_apis/wiki/wikis/{wiki.WikiName}/pages?recursionLevel={recursionLevel}&includeContent={includeContent}&api-version=7.1";
            using (var response = await httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var pages = JsonSerializer.Deserialize<WikiPages>(responseBody);
                foreach (var page in pages.subPages)
                {
                    await GetSubPages(page.url, httpClient, blobContainerClient, wiki.WikiName);
                }
            }
        }

        Console.WriteLine($"Number of documents imported: {docsImported}");
    }

    private async Task HandlePage(WikiPages page, string wikiIdentifier, BlobContainerClient blobContainerClient)
    {
        string cleanIdentifier = wikiIdentifier.Replace("%20", "_").Replace(" ","_");
        string cleanFileName = cleanIdentifier + "--" + page.path.Substring(1).Replace("/", "--").Replace(" ", "_");
        BlobClient blobClient = blobContainerClient.GetBlobClient(cleanFileName);

        BlobUploadOptions options = new BlobUploadOptions();
        options.Metadata = new Dictionary<string, string>
        {
            { "file_uri", page.remoteUrl }
        };

        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(page.content));
        await blobClient.UploadAsync(stream, options);
        docsImported++;
    }


    private async Task GetSubPages(string url, HttpClient client, BlobContainerClient blobContainerClient, string wikiIdentifier)
    {
        string newUrl = $"{url}?includeContent=true&recursionLevel=full";
            var subPageResponse = client.GetAsync(newUrl).Result.EnsureSuccessStatusCode();
            string subPageResponseBody = await subPageResponse.Content.ReadAsStringAsync();
            var subPage = JsonSerializer.Deserialize<WikiPages>(subPageResponseBody);

            if (subPage.content != "")
            {
                await HandlePage(subPage, wikiIdentifier, blobContainerClient);
            }

            if (subPage.subPages != null)
            {
                foreach (var subPageItem in subPage.subPages)
                {
                    try { 
                        await GetSubPages(subPageItem.url, client, blobContainerClient, wikiIdentifier);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing subpage: {ex.Message}\n{newUrl}");
                    }
                }
            }
       

    }
}
