namespace PriceCompare;

// Пропозиція магазину
public record Offer(string Shop, string Product, string Category, decimal Price, bool InStock);

// Позиція зі списку бажань покупця
public record WishItem(string Product, decimal MaxPrice);

// Рядок звіту: найдешевша пропозиція для товару
public record ReportItem(string Product, string Category, bool Available, string? Shop, decimal? Price);
