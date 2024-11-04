﻿namespace Flibusta.TelegramBot.Core.Helpers;

public static class EmojiHelper
{
    public static string GetNumber(int number)
    {
        //1️⃣2️⃣3️⃣4️⃣5️⃣6️⃣7️⃣8️⃣
        return number switch
        {
            1 => "1️⃣",
            2 => "2️⃣",
            3 => "3️⃣",
            4 => "4️⃣",
            5 => "5️⃣",
            6 => "6️⃣",
            7 => "7️⃣",
            8 => "8️⃣",
            _ => ""
        };
    }
}
