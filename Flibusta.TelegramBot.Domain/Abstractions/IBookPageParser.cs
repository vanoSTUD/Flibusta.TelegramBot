using Flibusta.TelegramBot.Domain.Entities;
using HtmlAgilityPack;

namespace Flibusta.TelegramBot.Domain.Abstractions;

public interface IBookPageParser
{
    public Book? Parse(HtmlNode bookNode);
}
