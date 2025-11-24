using PuppeteerSharp;

namespace TelegramBOT.Presentation.Rendering.Html
{
    public class HtmlToImageRenderer
    {
        public async Task<byte[]> RenderAsync(string html, int width, int height)
        {
            await new BrowserFetcher().DownloadAsync();

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox" }
            });

            var page = await browser.NewPageAsync();
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = width,
                Height = height
            });

            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Load }
            });

            var bytes = await page.ScreenshotDataAsync(new ScreenshotOptions
            {
                Type = ScreenshotType.Png
            });

            await browser.CloseAsync();
            return bytes;
        }
    }
}
