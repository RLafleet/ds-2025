using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _db;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = redis.GetDatabase();
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug("Received text with length: {Length}", text?.Length);

        if (string.IsNullOrEmpty(text))
        {
            return Page();
        }

        string id = Guid.NewGuid().ToString();

        _db.StringSet($"TEXT-{id}", text);

        double rank = CalculateRank(text);  
        _db.StringSet($"RANK-{id}", rank);

        double similarity = CheckTextUniqueness(text);
        _db.StringSet($"SIMILARITY-{id}", similarity);

        return Redirect($"summary?id={id}");
    }
    private double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var textElements = StringInfo.GetTextElementEnumerator(text);
        int totalElements = 0;
        int nonLetterElements = 0;

        while (textElements.MoveNext())
        {
            string element = textElements.GetTextElement();
            totalElements++;

            if (element.Length > 1 || !char.IsLetter(element[0]))
            {
                nonLetterElements++;
            }
        }

        return (double)nonLetterElements / totalElements;
    }

    private double CheckTextUniqueness(string text)
    {
        string textHash = TextToHash(text);
        bool isDuplicate = _db.SetContains("UNIQUE-TEXTS", textHash);

        if (!_db.SetContains("UNIQUE-TEXTS", textHash))
        {
            _db.SetAdd("UNIQUE-TEXTS", textHash);
            _db.StringSet($"TEXT-BY-HASH-{textHash}", text);
            return 0;
        }

        return isDuplicate ? 1 : 0;
    }

    private static string TextToHash(string text)
    {
        using var sha512 = SHA512.Create();
        byte[] bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(text));

        var builder = new StringBuilder();
        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2")); 
        }

        return builder.ToString();
    }
}