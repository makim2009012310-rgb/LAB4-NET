using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace PriceCompare;

internal class Program
{
    private static List<Offer> offers = new();

    // Друга колекція для Join — список бажань покупця
    private static readonly List<WishItem> wishList = new()
    {
        new("Xiaomi Redmi Note 13", 9000m),
        new("Навушники JBL Tune 520", 1900m),
        new("Lenovo IdeaPad 5", 30000m),
        new("iPhone 15", 40000m),
        new("Мишка Logitech M185", 400m)
    };

    private static bool echoInput;

    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        EnableDemoMode(args);

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("========== ПОРІВНЯННЯ ЦІН ==========");
            Console.WriteLine("1. Вихід");
            Console.WriteLine("2. Завантажити пропозиції (паралельно)");
            Console.WriteLine("3. Завантажити пропозиції (послідовно)");
            Console.WriteLine("4. Найдешевша пропозиція для кожного товару");
            Console.WriteLine("5. Статистика за категоріями");
            Console.WriteLine("6. Пошук у діапазоні цін");
            Console.WriteLine("7. Список бажань");
            Console.WriteLine("8. Зберегти звіт у JSON");
            Console.WriteLine("9. Товари, що є в усіх магазинах");
            Console.Write("Ваш вибір: ");

            string? choice = ReadInput();
            if (choice is "1" or null)
            {
                Console.WriteLine("До побачення!");
                return;
            }

            if (choice is "4" or "5" or "6" or "7" or "8" or "9" && offers.Count == 0)
            {
                Console.WriteLine("Спочатку завантажте пропозиції (пункт 2 або 3).");
                Console.WriteLine(new string('-', 60));
                continue;
            }

            switch (choice)
            {
                case "2": await LoadParallelAsync(); break;
                case "3": await LoadSequentialAsync(); break;
                case "4": ShowCheapest(); break;
                case "5": ShowCategoryStats(); break;
                case "6": SearchByPrice(); break;
                case "7": ShowWishList(); break;
                case "8": await SaveReportAsync(); break;
                case "9": ShowInAllShops(); break;
                default: Console.WriteLine("Невідомий пункт меню. Спробуйте ще раз."); break;
            }
            Console.WriteLine(new string('-', 60));
        }
    }

    // ---------- Завантаження ----------

    private static async Task LoadParallelAsync()
    {
        Console.WriteLine("Паралельне завантаження з 3 магазинів...");
        var sw = Stopwatch.StartNew();

        Task<List<Offer>> t1 = ShopSources.GetTechnoMarketAsync();
        Task<List<Offer>> t2 = ShopSources.GetCyfraPlusAsync();
        Task<List<Offer>> t3 = ShopSources.GetElektrosvitAsync();
        List<Offer>[] results = await Task.WhenAll(t1, t2, t3);

        sw.Stop();
        offers = results.SelectMany(list => list).ToList();
        Console.WriteLine($"Отримано {offers.Count} пропозицій за {sw.Elapsed.TotalSeconds:F2} с");
    }

    private static async Task LoadSequentialAsync()
    {
        Console.WriteLine("Послідовне завантаження з 3 магазинів...");
        var sw = Stopwatch.StartNew();

        var all = new List<Offer>();
        all.AddRange(await ShopSources.GetTechnoMarketAsync());
        all.AddRange(await ShopSources.GetCyfraPlusAsync());
        all.AddRange(await ShopSources.GetElektrosvitAsync());

        sw.Stop();
        offers = all;
        Console.WriteLine($"Отримано {offers.Count} пропозицій за {sw.Elapsed.TotalSeconds:F2} с");
    }

    // ---------- LINQ-запити ----------

    // Пункт 4: GroupBy + Any + MinBy, сортування за двома ключами, проєкція в ReportItem
    private static List<ReportItem> GetCheapestOffers()
    {
        return offers
            .GroupBy(o => o.Product)
            .Select(g =>
            {
                Offer? best = g.Any(o => o.InStock)
                    ? g.Where(o => o.InStock).MinBy(o => o.Price)
                    : null;
                return new ReportItem(g.Key, g.First().Category, best != null, best?.Shop, best?.Price);
            })
            .OrderBy(r => r.Category)
            .ThenBy(r => r.Product)
            .ToList();
    }

    private static void ShowCheapest()
    {
        Console.WriteLine("Найдешевші пропозиції (лише товари в наявності):");
        Console.WriteLine($"{"Товар",-24} {"Категорія",-10} {"Магазин",-12} {"Ціна, грн",12}");
        foreach (ReportItem r in GetCheapestOffers())
        {
            if (r.Available)
                Console.WriteLine($"{r.Product,-24} {r.Category,-10} {r.Shop,-12} {r.Price,12:N2}");
            else
                Console.WriteLine($"{r.Product,-24} {r.Category,-10} {"—",-12} {"немає в наявності",12}");
        }
    }

    // Пункт 5: GroupBy з агрегатами, проєкція в анонімний тип
    private static void ShowCategoryStats()
    {
        var stats = offers
            .GroupBy(o => o.Category)
            .Select(g => new
            {
                Category = g.Key,
                Count = g.Count(),
                Min = g.Min(o => o.Price),
                Avg = g.Average(o => o.Price),
                Max = g.Max(o => o.Price)
            })
            .OrderBy(x => x.Category);

        Console.WriteLine($"{"Категорія",-10} {"К-сть",6} {"Мін.",12} {"Середня",12} {"Макс.",12}");
        foreach (var s in stats)
        {
            string check = s.Min <= s.Max ? "" : "  (помилка: мін > макс)";
            Console.WriteLine($"{s.Category,-10} {s.Count,6} {s.Min,12:N2} {s.Avg,12:N2} {s.Max,12:N2}{check}");
        }
    }

    // Пункт 6: синтаксис запиту, Where + сортування за ціною, потім за магазином
    private static void SearchByPrice()
    {
        decimal min = ReadDecimal("Мінімальна ціна, грн: ");
        decimal max = ReadDecimal("Максимальна ціна, грн: ");
        if (min > max)
        {
            Console.WriteLine("Мінімальна ціна більша за максимальну — міняю їх місцями.");
            (min, max) = (max, min);
        }

        var found = from o in offers
                    where o.InStock && o.Price >= min && o.Price <= max
                    orderby o.Price, o.Shop
                    select new { o.Product, o.Shop, o.Price };

        Console.WriteLine($"Пропозиції в наявності від {min:N2} до {max:N2} грн:");
        if (!found.Any())
        {
            Console.WriteLine("Нічого не знайдено.");
            return;
        }
        foreach (var f in found)
            Console.WriteLine($"  {f.Price,12:N2} грн  {f.Shop,-12} {f.Product}");
    }

    // Пункт 7: Join списку бажань із пропозиціями за назвою товару
    private static void ShowWishList()
    {
        var matches = wishList
            .Join(offers,
                  w => w.Product,
                  o => o.Product,
                  (w, o) => new { Wish = w, Offer = o })
            .Where(x => x.Offer.InStock && x.Offer.Price <= x.Wish.MaxPrice)
            .ToList();

        foreach (WishItem w in wishList)
        {
            Console.WriteLine($"{w.Product} (до {w.MaxPrice:N2} грн):");
            var suitable = matches.Where(m => m.Wish == w).OrderBy(m => m.Offer.Price).ToList();
            if (suitable.Count == 0)
                Console.WriteLine("    немає пропозицій у наявності за цією ціною");
            else
                suitable.ForEach(m => Console.WriteLine($"    {m.Offer.Shop,-12} {m.Offer.Price,12:N2} грн"));
        }
    }

    // Пункт 9 (додатково): All — товар продається в кожному магазині
    private static void ShowInAllShops()
    {
        var shops = offers.Select(o => o.Shop).Distinct().ToList();
        var everywhere = offers
            .Select(o => o.Product)
            .Distinct()
            .Where(p => shops.All(s => offers.Any(o => o.Shop == s && o.Product == p)))
            .OrderBy(p => p);

        Console.WriteLine($"Товари, що є в асортименті всіх {shops.Count} магазинів:");
        foreach (string p in everywhere)
            Console.WriteLine($"  {p}");
    }

    // ---------- Збереження звіту ----------

    private static async Task SaveReportAsync()
    {
        string path = Path.Combine(Environment.CurrentDirectory, "report.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)   // кирилиця без \uXXXX
        };

        try
        {
            await using FileStream stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, GetCheapestOffers(), options);
            Console.WriteLine($"Звіт збережено: {path}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Не вдалося зберегти звіт: {ex.Message}");
        }
    }

    // ---------- Введення ----------

    private static decimal ReadDecimal(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = (ReadInput() ?? "").Replace(',', '.').Replace(" ", "");
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) && value >= 0)
                return value;
            Console.WriteLine("Введіть невід'ємне число, наприклад 1500");
        }
    }

    // Зчитує рядок; у демо-режимі виводить його на екран, як при ручному введенні
    private static string? ReadInput()
    {
        string? line = Console.ReadLine();
        if (echoInput) Console.WriteLine(line);
        return line;
    }

    // Демо-режим: введення з файлу сценарію (аргумент або demo-input.txt поруч з exe)
    private static void EnableDemoMode(string[] args)
    {
        string script = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "demo-input.txt");
        if (File.Exists(script) && new FileInfo(script).Length > 0)
        {
            Console.SetIn(new StreamReader(script, Encoding.UTF8));
            echoInput = true;
        }
    }
}
