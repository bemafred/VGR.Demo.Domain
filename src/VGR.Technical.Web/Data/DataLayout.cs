namespace VGR.Technical.Web.Data;

/// <summary>
/// Gemensam HTML-layout för /data-sidorna. Samma dark-theme som /domain och /api.
/// </summary>
internal static class DataLayout
{
    public static string Wrap(string title, string breadcrumbHtml, string bodyHtml) => $$"""
        <!DOCTYPE html>
        <html lang="sv">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{Esc(title)}} — VGR.Demo.Domain</title>
            <link rel="icon" href="/favicon.svg" type="image/svg+xml">
            <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }

                body {
                    font-family: system-ui, -apple-system, sans-serif;
                    background: #0a0a0a;
                    color: #e0e0e0;
                    min-height: 100vh;
                    padding: 2rem;
                    max-width: 72rem;
                    margin: 0 auto;
                }

                header {
                    display: flex;
                    align-items: center;
                    gap: 1rem;
                    margin-bottom: 2rem;
                    padding-bottom: 1rem;
                    border-bottom: 1px solid #1a1a1a;
                }

                header h1 {
                    font-size: 1.5rem;
                    font-weight: 300;
                    color: #fff;
                    flex: 1;
                }

                header a, .breadcrumb a {
                    color: #0bd6ea;
                    text-decoration: none;
                    font-size: 0.85rem;
                }

                .breadcrumb {
                    margin-bottom: 1.5rem;
                    font-size: 0.85rem;
                    color: #666;
                }

                .breadcrumb a:hover { text-decoration: underline; }

                table {
                    width: 100%;
                    border-collapse: collapse;
                    font-size: 0.85rem;
                    margin-top: 0.5rem;
                }

                thead th {
                    text-align: left;
                    color: #555;
                    font-weight: 400;
                    font-size: 0.75rem;
                    padding: 0.4rem 0.75rem;
                    border-bottom: 1px solid #1a1a1a;
                }

                tbody td {
                    padding: 0.4rem 0.75rem;
                    border-bottom: 1px solid #111;
                }

                tbody tr:hover { background: #111; }

                a.entity-link {
                    color: #60a5fa;
                    text-decoration: none;
                }

                a.entity-link:hover { text-decoration: underline; }

                .mono {
                    font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                    font-size: 0.8rem;
                }

                .badge {
                    font-size: 0.65rem;
                    padding: 0.15rem 0.5rem;
                    border-radius: 0.15rem;
                    text-transform: uppercase;
                    letter-spacing: 0.05em;
                }

                .badge.aggregate { background: #1a3320; color: #4ade80; }
                .badge.entity { background: #1a2733; color: #60a5fa; }

                .tag {
                    font-size: 0.6rem;
                    padding: 0.1rem 0.4rem;
                    border-radius: 0.15rem;
                    margin-left: 0.25rem;
                }

                .tag.ro { background: #1a1a2a; color: #818cf8; }
                .tag.nav { background: #1a2a1a; color: #4ade80; }
                .tag.st { background: #1a1a1a; color: #888; }

                .empty { color: #555; font-style: italic; padding: 1rem 0; }

                section { margin-bottom: 2rem; }

                section > h2 {
                    font-size: 1.1rem;
                    font-weight: 400;
                    color: #888;
                    text-transform: uppercase;
                    letter-spacing: 0.1em;
                    margin-bottom: 0.75rem;
                    padding-bottom: 0.25rem;
                    border-bottom: 1px solid #1a1a1a;
                }

                .method-form {
                    border: 1px solid #1a1a1a;
                    border-radius: 0.25rem;
                    background: #0f0f0f;
                    padding: 1rem;
                    margin-bottom: 0.75rem;
                }

                .method-form h3 {
                    font-size: 0.9rem;
                    font-weight: 400;
                    color: #ccc;
                    margin-bottom: 0.75rem;
                    font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                }

                .method-form label {
                    display: block;
                    font-size: 0.75rem;
                    color: #888;
                    margin-bottom: 0.2rem;
                    margin-top: 0.5rem;
                }

                .method-form input, .method-form select {
                    width: 100%;
                    max-width: 24rem;
                    padding: 0.35rem 0.5rem;
                    background: #1a1a1a;
                    border: 1px solid #2a2a2a;
                    border-radius: 0.2rem;
                    color: #e0e0e0;
                    font-size: 0.8rem;
                    font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                }

                .method-form input:focus {
                    outline: none;
                    border-color: #0bd6ea;
                }

                .method-form button {
                    margin-top: 0.75rem;
                    padding: 0.4rem 1.25rem;
                    background: #1a3a3e;
                    border: 1px solid #0bd6ea;
                    border-radius: 0.2rem;
                    color: #0bd6ea;
                    font-size: 0.8rem;
                    cursor: pointer;
                    transition: background 0.2s;
                }

                .method-form button:hover { background: #0bd6ea20; }

                .result-banner {
                    margin-top: 0.5rem;
                    padding: 0.5rem 0.75rem;
                    border-radius: 0.2rem;
                    font-size: 0.8rem;
                    font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                    white-space: pre-wrap;
                    display: none;
                    position: relative;
                }

                .result-banner .close-btn {
                    position: absolute;
                    top: 0.3rem;
                    right: 0.5rem;
                    background: none;
                    border: none;
                    color: inherit;
                    font-size: 1rem;
                    cursor: pointer;
                    opacity: 0.6;
                    padding: 0 0.3rem;
                }

                .result-banner .close-btn:hover { opacity: 1; }

                .result-banner.success { background: #1a3320; color: #4ade80; border: 1px solid #2a5530; display: block; }
                .result-banner.error { background: #331a1a; color: #f87171; border: 1px solid #552a2a; display: block; }

                footer {
                    margin-top: 3rem;
                    padding-top: 1rem;
                    border-top: 1px solid #1a1a1a;
                    text-align: center;
                    opacity: 0.4;
                    transition: opacity 0.3s;
                }

                footer:hover { opacity: 0.8; }
                footer img { width: 36px; height: 36px; }
            </style>
        </head>
        <body>
            <header>
                <h1>{{Esc(title)}}</h1>
                <a href="/">&larr; Tillbaka</a>
            </header>
            <div class="breadcrumb">{{breadcrumbHtml}}</div>
            {{bodyHtml}}
            <footer>
                <a href="https://github.com/bemafred/sky-omega">
                    <img src="/edgar-badge.svg" alt="Sky Omega" title="Edgar">
                </a>
            </footer>
        </body>
        </html>
        """;

    public static string Esc(string? s) =>
        (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
