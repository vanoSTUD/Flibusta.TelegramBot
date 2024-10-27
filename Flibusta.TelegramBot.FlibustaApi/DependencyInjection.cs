using Flibusta.TelegramBot.Domain.Abstractions;
using Flibusta.TelegramBot.Domain.Entities;
using Flibusta.TelegramBot.FlibustaApi.Parsers;
using Microsoft.Extensions.DependencyInjection;

namespace Flibusta.TelegramBot.FlibustaApi;

public static class DependencyInjection
{
	public static void AddFlibustaApi(this IServiceCollection services)
	{
		services.AddSingleton<IFlibustaApi, FlibustaApi>();

		services.AddSingleton<IPageParser<Book>, BookParser>();
		services.AddSingleton<IPageParser<List<Book>>, BookCollectionParser>();
	}
}
