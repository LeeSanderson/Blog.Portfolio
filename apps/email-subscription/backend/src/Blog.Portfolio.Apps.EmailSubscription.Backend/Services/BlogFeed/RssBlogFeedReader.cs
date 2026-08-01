using System.ServiceModel.Syndication;
using System.Xml;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.BlogFeed;

public sealed class RssBlogFeedReader : IBlogFeedReader
{
    private static readonly Uri BlogRssUrl = new("https://www.sixsideddice.com/Blog/rss.xml");

    private readonly HttpClient _httpClient;

    public RssBlogFeedReader(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<BlogPost>> GetPostsAsync(CancellationToken cancellationToken)
    {
        var stream = await _httpClient.GetStreamAsync(BlogRssUrl, cancellationToken);
        await using (stream.ConfigureAwait(false))
        {
            using var xmlReader = XmlReader.Create(stream);
            var feed = SyndicationFeed.Load(xmlReader);

            return feed.Items.Select(ToBlogPost).ToList();
        }
    }

    private static BlogPost ToBlogPost(SyndicationItem item) =>
        new(
            item.Title?.Text ?? string.Empty,
            item.Links.FirstOrDefault()?.Uri.ToString() ?? string.Empty,
            item.Summary?.Text ?? string.Empty,
            item.PublishDate);
}
