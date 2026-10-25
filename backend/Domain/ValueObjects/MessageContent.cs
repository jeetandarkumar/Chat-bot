using System;
using System.Text.Json.Serialization;

namespace ChatApp.Domain.ValueObjects;

public record MessageContent
{
    public string? Text { get; init; }
    public MediaContent? Media { get; init; }
    public LocationContent? Location { get; init; }
    public ContactContent? Contact { get; init; }

    public MessageContent() { }

    public static MessageContent FromText(string text) => new() { Text = text };
    public static MessageContent FromMedia(MediaContent media) => new() { Media = media };
    public static MessageContent FromLocation(LocationContent location) => new() { Location = location };
    public static MessageContent FromContact(ContactContent contact) => new() { Contact = contact };

    public bool HasText => !string.IsNullOrWhiteSpace(Text);
    public bool HasMedia => Media != null;
    public bool HasLocation => Location != null;
    public bool HasContact => Contact != null;
}

public record MediaContent
{
    public string Url { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? ThumbnailUrl { get; init; }
}

public record LocationContent
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Address { get; init; }
    public string? Name { get; init; }
}

public record ContactContent
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? LastName { get; init; }
    public string? Email { get; init; }
}
