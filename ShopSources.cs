namespace PriceCompare;

// Три асинхронні джерела даних — імітація запитів до серверів магазинів
public static class ShopSources
{
    public static async Task<List<Offer>> GetTechnoMarketAsync()
    {
        await Task.Delay(1500);
        const string s = "ТехноМаркет";
        return new List<Offer>
        {
            new(s, "Lenovo IdeaPad 5", "Ноутбуки", 28999m, true),
            new(s, "ASUS VivoBook 15", "Ноутбуки", 21499m, true),
            new(s, "HP Pavilion 14", "Ноутбуки", 26999m, false),
            new(s, "Samsung Galaxy A55", "Смартфони", 15999m, true),
            new(s, "Xiaomi Redmi Note 13", "Смартфони", 8799m, true),
            new(s, "iPhone 15", "Смартфони", 37999m, false),
            new(s, "Навушники JBL Tune 520", "Аксесуари", 1999m, true),
            new(s, "Павербанк Xiaomi 20000", "Аксесуари", 1299m, true),
            new(s, "Мишка Logitech M185", "Аксесуари", 449m, true)
        };
    }

    public static async Task<List<Offer>> GetCyfraPlusAsync()
    {
        await Task.Delay(1000);
        const string s = "ЦифраПлюс";
        return new List<Offer>
        {
            new(s, "Lenovo IdeaPad 5", "Ноутбуки", 29499m, true),
            new(s, "ASUS VivoBook 15", "Ноутбуки", 20999m, false),
            new(s, "Samsung Galaxy A55", "Смартфони", 15499m, true),
            new(s, "Xiaomi Redmi Note 13", "Смартфони", 8999m, true),
            new(s, "iPhone 15", "Смартфони", 38499m, false),
            new(s, "Навушники JBL Tune 520", "Аксесуари", 1849m, true),
            new(s, "Павербанк Xiaomi 20000", "Аксесуари", 1199m, false),
            new(s, "Мишка Logitech M185", "Аксесуари", 399m, true)
        };
    }

    public static async Task<List<Offer>> GetElektrosvitAsync()
    {
        await Task.Delay(2000);
        const string s = "Електросвіт";
        return new List<Offer>
        {
            new(s, "Lenovo IdeaPad 5", "Ноутбуки", 28499m, false),
            new(s, "HP Pavilion 14", "Ноутбуки", 27499m, true),
            new(s, "Samsung Galaxy A55", "Смартфони", 16299m, true),
            new(s, "Xiaomi Redmi Note 13", "Смартфони", 8599m, true),
            new(s, "iPhone 15", "Смартфони", 36999m, false),
            new(s, "Навушники JBL Tune 520", "Аксесуари", 2099m, true),
            new(s, "Павербанк Xiaomi 20000", "Аксесуари", 1349m, true),
            new(s, "Мишка Logitech M185", "Аксесуари", 429m, true)
        };
    }
}
