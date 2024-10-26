using Flibusta.TelegramBot.Domain.Abstractions;
using Flibusta.TelegramBot.Domain.Entities;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flibusta.TelegramBot.FlibustaApi.Utils;

public class BookPageParser : IBookPageParser
{
    private readonly ILogger<BookPageParser> _logger;

    public BookPageParser(ILogger<BookPageParser> logger)
    {
        _logger = logger;
    }

    public Book? Parse(HtmlNode bookPageNode, int bookId)
    {
        Book? book = null;

        try
        {
            book = new Book()
            {
                Id = bookId,
                Title = GetTitle(bookPageNode),
                Authors = GetAuthors(bookPageNode),
                AdditionDate = GetAdditionDate(bookPageNode),
                Genres = GetGenres(bookPageNode),
                PublicationYear = GetPublicationYear(bookPageNode)
            };
        }
        catch(Exception ex)
        {
            _logger.LogError("Exception in BookPageParser: {e}", ex.Message);
            return book;
        }

    }

    public List<Author> GetAuthors(HtmlNode bookPageNode)
    {
        return bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'author')]//span[@class='row_content']").ChildNodes
            .Where(node => node.Name == "a" && !string.IsNullOrWhiteSpace(node.InnerText))
                .Select(item => new Author()
                {
                    Id = int.Parse(string.Join("", item.Attributes["href"].Value[3..])),
                    Name = item.InnerText
                }).ToList();
    }

    private string GetTitle(HtmlNode bookPageNode)
    {
        return bookPageNode.SelectSingleNode($"//div[@class='b_biblio_book_top']/div/h1").InnerText;
    }

    private string GetPublicationYear(HtmlNode bookPageNode)
    {
        return bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'year_public')]//span[@class='row_content']").InnerText;
    }

    private DateTime GetAdditionDate(HtmlNode bookPageNode)
    {
        return DateTime.Parse(bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'date_add')]//span[@class='row_content']").InnerText);
    }

    private List<string> GetGenres(HtmlNode bookPageNode)
    {
        return bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'genre')]//span[@class='row_content']").ChildNodes
            .Where(node => node.Name == "a" && !string.IsNullOrWhiteSpace(node.InnerText))
                .Select(node => node.InnerText).ToList();
    }
}

