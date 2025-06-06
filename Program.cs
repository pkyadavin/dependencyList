using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Net.Http.Headers;
using System.Web;
class Program
{
    static async Task Main(string[] args)
    {
        //  https://URL/gitlab/api/v4/projects?language=c%23&per_page=100&page=2
        //  this url gives list of all projectID's 
        string[] RepoList = { "11510", "16104", "14324", "14515", "16019", "11498", "17615", "16907", "11556", "11558", "11570", "11552", "11554", "16686", "16706", "16670", "16701", "16672", "16709", "12765", "12767", "15047", "14534", "11542", "13018", "16487", "16113", "16129", "16131", "16132", "16133", "12774", "11526", "11605", "11540", "13020", "11494", "11532", "11534", "12629", "13069", "11538", "11593", "17667", "17649", "13078", "17586", "14342", "13080", "12443", "17632", "15519", "15159", "11603", "11546", "11492", "15861", "14885", "16095", "11562", "11524", "17131", "16864", "17558", "11522", "11486", "11488", "17557", "11550", "17559", "11587", "11575", "16973", "11482", "15831", "15690", "11585", "12753", "12793", "16603", "11484", "11518", "11502", "16176", "12303", "11564", "11589", "11548", "11609", "12791", "11496", "11508", "17634", "15573", "11560", "9666", "11536", "16703", "11500", "12468", "12289", "11512", "11607", "14666", "11516", "11678", "11581", "11520", "16597", "11591", "14404", "16443", "12759", "12761", "12763", "15877", "11514", "14406", "16360", "15178", "15756", "11504", "11506", "12305", "15176", "11566", "11195", "14581", "11599", "14845", "12731", "11943", "16117", "14847", "11888", "11884", "11263", "16121", "12771", "12776", "12795", "12780", "12778", "12769", "17611", "16122", "15148", "12745", "16124", "11945", "14843", "17214", "11597", "11579", "11583", "11568", "11544", "14663", "11573", "11595", "11577", "11601", "16058", "16595", "11528", "14838", "14583", "16967", "17636", "14622", "11698" };

        string token = "xyzabc"; // Load token from environment variable
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("GitHub token is missing.");
            return;
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                 (sender, cert, chain, sslPolicyErrors) => true
        };

        using (HttpClient client = new HttpClient(handler))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                if (RepoList.Length == 0)
                {
                    Console.WriteLine("No repositories found.");
                }
                else
                {
                    // Loop through the repositories
                    foreach (var repo in RepoList)
                    {
                        Console.WriteLine($"ProjectID: {repo}");
                        // Check if the repository contains any file with "cs" in its name
                        bool containsCsFile = await CheckForCsFile(client, repo, "");
                    }
                }

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"HTTP Error: {e.Message}");
                Console.WriteLine($"HTTP Error: {e.InnerException}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }
    }

    // Method to check if the repository contains a file with "cs" in its name recursively
    static async Task<bool> CheckForCsFile(HttpClient client, string projectId, string path)
    {
        string contentsUrl = $"https://URL/gitlab/api/v4/projects/{projectId}/search?scope=blobs&search=.csproj";
        try
        {
            // Fetch the contents of the current folder
            HttpResponseMessage response = await client.GetAsync(contentsUrl);
            response.EnsureSuccessStatusCode(); // Ensure a successful status code

            // Read the response content as string
            string responseContent = await response.Content.ReadAsStringAsync();

            // Parse the JSON response (which lists the files and subfolders in the directory)
            JArray files = JArray.Parse(responseContent);

            // Loop through each file/folder in the current directory
            foreach (var file in files)
            {
                path = file["path"].ToString();
                // Console.WriteLine($"path: {path}");   
                if (path.Contains("csproj", StringComparison.OrdinalIgnoreCase))
                {
                    if (!path.Contains("Test", StringComparison.OrdinalIgnoreCase))
                    {
                        await ListDependenciesInCsproj(client, projectId, path);
                    }
                }
            }

            return false; // No file with "cs" in its name found in the current directory or subdirectories
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Failed to fetch content from {contentsUrl}");
            // If the request fails (e.g., 404 or other errors), we consider that no file with "cs" is found
            return false;
        }
    }

    // Method to list dependencies in `.csproj` files
    static async Task ListDependenciesInCsproj(HttpClient client, string projectId, string path)
    {
        Logger logger = new Logger("log.txt");
        path = HttpUtility.UrlEncode(path);
        string contentsUrl = $"https://URL/gitlab/api/v4/projects/{projectId}/repository/files/{path}/raw";
        string folder = @"C:\Temp\";
        // Filename
        string fileName = "obsolesence.txt";
        // Fullpath. You can direct hardcode it if you like.
        string fullPath = folder + fileName;
        try
        {
            // Fetch the contents of the current folder           
            HttpResponseMessage response = await client.GetAsync(contentsUrl);
            response.EnsureSuccessStatusCode(); // Ensure a successful status code

            // Read the response content as string
            string csprojContent = await response.Content.ReadAsStringAsync();

            XDocument csprojDoc = XDocument.Parse(csprojContent);

            var packageReferences = csprojDoc.Descendants()
                .Where(d => d.Name.LocalName == "PackageReference")
                .Select(d => new
                {
                    PackageName = d.Attribute("Include")?.Value,
                    Version = d.Attribute("Version")?.Value
                            ?? d.Elements().FirstOrDefault(e => e.Name.LocalName == "Version")?.Value
                });

            var referenceElements = csprojDoc.Descendants()
                .Where(d => d.Name.LocalName == "Reference")
                .Select(d =>
                {
                    var include = d.Attribute("Include")?.Value;
                    string packageName = include?.Split(',').FirstOrDefault()?.Trim();
                    string version = include?.Split(',')?
                        .FirstOrDefault(p => p.Trim().StartsWith("Version=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=').Last().Trim();

                    return new
                    {
                        PackageName = packageName,
                        Version = version
                    };
                });

            var allReferences = packageReferences.Concat(referenceElements);

            // List the dependencies
            if (allReferences.Any())
            {
                Console.WriteLine($"");
                foreach (var package in allReferences)
                {                     
                        string message = $"ProjectId {projectId} , Path {HttpUtility.UrlDecode(path)}, {package.PackageName} {package.Version}";
                        logger.Log(message);
                        Console.WriteLine(message);     
                }
            }
            else
            {
                Console.WriteLine($"  No dependencies found in this .csproj {HttpUtility.UrlDecode(path)}.");
            }
        }
        catch (Exception ex)
        {
            // Handle the error gracefully if the folder content can't be fetched
            Console.WriteLine($"  Failed to fetch content from {path}");
        }
    }
}


