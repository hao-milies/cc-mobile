using System.Text.Json;
using CCMobile.Models;

namespace CCMobile.Services
{
    public static class SessionCart
    {
        private const string Key = "CCMobile.Cart";
        public const int MaxQuantity = 99;
        public const int MaxLines = 50;

        public static Dictionary<int, int> Read(ISession session)
        {
            var json = session.GetString(Key);
            if (string.IsNullOrEmpty(json)) return new();
            try { return JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new(); }
            catch (JsonException) { session.Remove(Key); return new(); }
        }

        public static void Write(ISession session, Dictionary<int, int> cart)
        {
            if (cart.Count == 0) session.Remove(Key);
            else session.SetString(Key, JsonSerializer.Serialize(cart));
        }

        // Loại hàng không còn bán, giới hạn số lượng theo tồn kho mới nhất.
        public static bool Normalize(Dictionary<int, int> cart, IEnumerable<Product> products)
        {
            var byId = products.ToDictionary(p => p.Id);
            var changed = false;
            foreach (var id in cart.Keys.ToList())
            {
                if (!byId.TryGetValue(id, out var product) ||
                    !product.IsActive || product.StockQuantity <= 0 ||
                    product.Price <= 0 || cart[id] <= 0)
                {
                    cart.Remove(id);
                    changed = true;
                    continue;
                }
                var quantity = Math.Min(cart[id], Math.Min(MaxQuantity, product.StockQuantity));
                if (quantity != cart[id]) { cart[id] = quantity; changed = true; }
            }
            return changed;
        }
    }
}
