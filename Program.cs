using Microsoft.Playwright;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
});
var context = await browser.NewContextAsync();

var page = await context.NewPageAsync();

await page.GotoAsync("https://dijital.spoileryayinlari.com/Cozumler/KitapListView?layout=True&seviyeId=0&dersId=0");

int i = 1;

var kitapDivs = await page.QuerySelectorAllAsync(".kitapDiv");
for (int w = 0; w < kitapDivs.Count(); w++)
{
    if (w != 0)
    {
        await page.GotoAsync("https://dijital.spoileryayinlari.com/Cozumler/KitapListView?layout=True&seviyeId=0&dersId=0");

        kitapDivs = await page.QuerySelectorAllAsync(".kitapDiv");
    }
    var link = await kitapDivs[w].QuerySelectorAsync("a");
    var href = await link!.GetAttributeAsync("href");
    var spanElement = await kitapDivs[w].QuerySelectorAsync(".kitapAdi");
    string text = await spanElement!.InnerTextAsync();
    await page.GotoAsync($"https://dijital.spoileryayinlari.com{href}");
    await Task.Delay(10000);
    var btnElements = await page.QuerySelectorAllAsync(".btn");
    for (int z = 0; z < btnElements.Count(); z++)
    {
        if (z != 0)
        {
            await page.GotoAsync($"https://dijital.spoileryayinlari.com{href}");
            btnElements = await page.QuerySelectorAllAsync(".btn");
            i = 1;
        }
        var breadcrumbSecond = await page.QuerySelectorAsync(".breadcrumb li:nth-child(2) a");
        var breadcrumbThird = await page.QuerySelectorAsync(".breadcrumb li:nth-child(3) a");

        string breadFirstName = await breadcrumbSecond!.InnerTextAsync();
        string breadSecondName = await breadcrumbThird!.InnerTextAsync();
        string breadFileName = ($"{TurkishCharacterToEnglish(breadFirstName)}/{TurkishCharacterToEnglish(breadSecondName)}");

        var secondPagehref = await btnElements[z].GetAttributeAsync("href");
        await page.GotoAsync($"https://dijital.spoileryayinlari.com/{secondPagehref}");
        var videoElements = await page.QuerySelectorAllAsync("[data-video]");

        foreach (var videoElement in videoElements)
        {
            var videoUrl = await videoElement.EvaluateAsync<string>("element => element.getAttribute('data-video')");
            var lastBredCrumb = await page.QuerySelectorAsync(".breadcrumb > li:nth-child(3)");
            string lastBreadName = await lastBredCrumb!.InnerTextAsync();
            string breadNameFinal = $"/{TurkishCharacterToEnglish(lastBreadName)}";
            if (videoUrl == null)
            {
                Console.WriteLine($"video source not found");
            }
            else
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("{username}:{password}")));
                    try
                    {
                        CredentialCache mycache = new CredentialCache();
                        Uri myUri = new Uri(videoUrl);
                        mycache.Add(myUri, "Basic", new NetworkCredential("username", "password"));
                        HttpWebRequest? request = (HttpWebRequest)WebRequest.Create(videoUrl);
                        request.UseDefaultCredentials = true;
                        request!.Proxy!.Credentials = CredentialCache.DefaultCredentials;
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Your user agent string here");
                        httpClient.DefaultRequestHeaders.Add("Referer", "https://dijital.spoileryayinlari.com/");
                        var filePath = $"Kitaplar/{breadFileName}/{breadNameFinal}/{i}.mp4";
                        Console.WriteLine(filePath);
                        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                        }
                        using (var response = await httpClient.GetAsync(videoUrl))
                        {
                            using (var fileStream = File.OpenWrite(filePath))
                            {
                                await response.Content.CopyToAsync(fileStream);
                            }
                        }
                        i +=1;
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine($"er: {ex}");
                    }
                }
            }
        }

    }
}

static string TurkishCharacterToEnglish(string text)
{
    char[] turkishChars = { 'ı', 'ğ', 'İ', 'Ğ', 'ç', 'Ç', 'ş', 'Ş', 'ö', 'Ö', 'ü', 'Ü', ':', '.', ',', 'â', '\"', '\'', 'Â', ' ' };
    char[]? englishChars = { 'i', 'g', 'I', 'G', 'c', 'C', 's', 'S', 'o', 'O', 'u', 'U', '-', '_', '-', 'a', '-', '-', '-', 'A', '_' };

    for (int i = 0; i < turkishChars.Length; i++)
        text = text.Replace(turkishChars[i], englishChars[i]);

    return text;
}