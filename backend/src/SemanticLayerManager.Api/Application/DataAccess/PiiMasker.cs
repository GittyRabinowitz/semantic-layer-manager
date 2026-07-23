namespace SemanticLayerManager.Api.Application.DataAccess;

/// <summary>
/// Masks personally-identifiable values before they leave the semantic layer.
/// Keeps a short, recognisable prefix and (for emails) the domain, hiding the rest.
/// </summary>
public static class PiiMasker
{
    public static object? Mask(object? value)
    {
        if (value is null)
            return null;

        var text = value.ToString();
        if (string.IsNullOrEmpty(text))
            return text;

        var at = text.IndexOf('@');
        if (at > 0)
        {
            // Email: keep up to two chars of the local part, keep the domain.
            var local = text[..at];
            var shown = local.Length <= 2 ? local[..1] : local[..2];
            return shown + new string('*', Math.Max(3, local.Length - shown.Length)) + text[at..];
        }

        // Generic: keep up to two leading chars, mask the remainder.
        var keep = Math.Min(2, text.Length);
        return text[..keep] + new string('*', Math.Max(3, text.Length - keep));
    }
}
