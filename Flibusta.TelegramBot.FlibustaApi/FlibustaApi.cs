using Flibusta.TelegramBot.Domain.Abstractions;
using Flibusta.TelegramBot.Domain.Entities;
using HtmlAgilityPack;

namespace Flibusta.TelegramBot.FlibustaApi;

public class FlibustaApi
{
    private const string FlibustaUri = "https://flibusta.club";
    private readonly IBookPageParser _bookParser;

    public FlibustaApi(IBookPageParser bookParser)
    {
        _bookParser = bookParser;
    }

    public async Task<List<Book>> GetBooksByNameAsync(string bookTitle)
    {
        int pageCount = await GetPageCountAsync(bookTitle);
        var books = new List<Book>();

        for(int i = 0; i < pageCount; i++)
        {
            var foundedBooks = await GetBooksByPageAsync(bookTitle, i);
            books.AddRange(foundedBooks);
        }

        return books;
    }

    public async Task<Book> GetBookByIdAsync(int id)
    {
        string requestUrl = $"{FlibustaUri}/b/{id}";
        var web = new HtmlWeb();
        var document = await web.LoadFromWebAsync(requestUrl);
        var documentNode = document.DocumentNode;

        var bookNode = documentNode.SelectSingleNode($"//div[@class='b_biblio_book']");
        var bookTitle = documentNode.SelectSingleNode($"//div[@class='b_biblio_book_top']/div/h1").InnerText;
        var additionDate = DateTime.Parse(documentNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'date_add')]//span[@class='row_content']").InnerText);

        List<Author> authors = documentNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'author')]//span[@class='row_content']").ChildNodes
            .Where(node => node.Name == "a" && !string.IsNullOrWhiteSpace(node.InnerText))
                .Select(item => new Author()
                {
                    Id = int.Parse(string.Join("", item.Attributes["href"].Value[3..])),
                    Name = item.InnerText
                }).ToList();

        List<string> genres = documentNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'genre')]//span[@class='row_content']").ChildNodes
            .Where(node => node.Name == "a" && !string.IsNullOrWhiteSpace(node.InnerText))
                .Select(node => node.InnerText).ToList();

        var publicationYear = documentNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'year_public')]//span[@class='row_content']").InnerText;

        var book = new Book()
        {
            Id = id,
            Title = bookTitle,
            Authors = authors,
            AdditionDate = additionDate,
            Genres = genres,
            PublicationYear = publicationYear
        };

        return book;
    }

    private static HtmlNodeCollection GetRowContent(HtmlNode bookDescriptionNode)
    {
        return bookDescriptionNode.SelectNodes(GetXPath("row_content"));
    }

    private static string GetContainsXPath(string className)
    {
        return $"//*[contains(concat(' ', normalize-space(@class), ' '), '{className}')]";
    }

    private static string GetXPath(string className)
    {
        return $"//*[@class='{className}']";
    }


    private static async Task<List<Book>> GetBooksByPageAsync(string bookTitle, int page)
    {
        string requestUrl = $"{FlibustaUri}/booksearch?page={page}&ask={bookTitle}";
        var web = new HtmlWeb();
        var document = await web.LoadFromWebAsync(requestUrl);

        var bookElements = document.DocumentNode.SelectSingleNode("//*[@id='main']")
            .ChildNodes
                .Where(x => x.Name == "li" && x.InnerHtml.Contains("href=\"/b/"));

        var books = new List<Book>();

        foreach ( var element in bookElements )
        {
            var title = element.FirstChild.InnerText;
            var authorName = element.LastChild.InnerText;
            (var bookId, var authorId) = GetIds(element);

            var book = new Book()
            {
                Id = bookId,
                Title = title,
                Authors =
                [
                    new Author()
                    {
                        Id = authorId,
                        Name = authorName,
                    }
                ]
            };

            books.Add(book);
        }

        return books;
    }

    private static async Task<int> GetPageCountAsync(string bookTitle)
    {
        string requestUrl = $"{FlibustaUri}/booksearch?ask={bookTitle}";

        var web = new HtmlWeb();
        var document = await web.LoadFromWebAsync(requestUrl);
        var lastPageItem = document.DocumentNode.SelectSingleNode("//*[contains(concat(' ', normalize-space(@class), ' '), 'pager-last')]");
        int pageNumberIndex = 17;

        if (lastPageItem != null)
        {
            var href = lastPageItem.FirstChild.Attributes["href"].Value;
            int pageCount = int.Parse(href[pageNumberIndex].ToString());

            return pageCount;
        }

        return 0;
    }

    /// <returns>(bookId, authorId)</returns>
    private static (int, int) GetIds(HtmlNode bookNode)
    {
        var items = bookNode.ChildNodes.Where(node => node.Name == "a");
        var bookId = int.Parse(string.Join("", items.First().Attributes["href"].Value[3..]));
        var authorId = int.Parse(string.Join("", items.Last().Attributes["href"].Value[3..]));

        return (bookId, authorId);
    }
}
