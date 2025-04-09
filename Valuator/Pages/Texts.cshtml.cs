using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages
{
    public class TextsModel : PageModel
    {
        private readonly IDatabase _redis;

        public TextsModel(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public List<TextEntry> Entries { get; set; } = new List<TextEntry>();

        public void OnGet()
        {
            var server = _redis.Multiplexer.GetServer("localhost:6379");
            var keys = server.Keys(pattern: "TEXT-*");

            foreach (var key in keys)
            {
                var id = key.ToString().Split('-')[1];
                var text = _redis.StringGet(key);
                var rank = _redis.StringGet($"RANK-{id}");
                var similarity = _redis.StringGet($"SIMILARITY-{id}");

                Entries.Add(new TextEntry
                {
                    Id = id,
                    Text = text,
                    Rank = rank.HasValue ? double.Parse(rank) : 0,
                    Similarity = similarity.HasValue ? double.Parse(similarity) : 0
                });
            }
        }

        public class TextEntry
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public double Rank { get; set; }
            public double Similarity { get; set; }
        }
    }
}