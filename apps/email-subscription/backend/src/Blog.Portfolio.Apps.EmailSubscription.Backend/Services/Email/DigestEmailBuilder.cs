using System.Text.Encodings.Web;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.BlogFeed;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
using Microsoft.Extensions.Options;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;

public sealed class DigestEmailBuilder
{
    private readonly SubscriberLinkBuilder _linkBuilder;
    private readonly EmailSubscriptionOptions _options;

    public DigestEmailBuilder(SubscriberLinkBuilder linkBuilder, IOptions<EmailSubscriptionOptions> options)
    {
        _linkBuilder = linkBuilder;
        _options = options.Value;
    }

    public SendEmailMessage Build(Subscriber subscriber, IEnumerable<BlogPost> posts)
    {
        var unsubscribeLink = _linkBuilder.Build(_options.UnsubscribePageUrl, subscriber.Id, TokenPurpose.Unsubscribe);

        var postsHtml = string.Concat(posts.Select(RenderPost));

        var html = $"""
            <p>New posts on sixsideddice.com this week:</p>
            <ul>{postsHtml}</ul>
            <p><a href="{unsubscribeLink}">Unsubscribe</a></p>
            """;

        return new SendEmailMessage(subscriber.Email, "New posts on sixsideddice.com", html);
    }

    private static string RenderPost(BlogPost post)
    {
        var link = HtmlEncoder.Default.Encode(post.Link);
        var title = HtmlEncoder.Default.Encode(post.Title);
        var description = HtmlEncoder.Default.Encode(post.Description);

        return $"""<li><a href="{link}">{title}</a><p>{description}</p></li>""";
    }
}
