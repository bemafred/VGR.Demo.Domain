using System.Reflection;

namespace VGR.Technical.Web;

/// <summary>
/// Renderar indexsidan — systemets landningssida med domänidentitet.
/// </summary>
internal static class IndexPage
{
    public static string Render(Assembly[] domainAssemblies)
    {
        var assemblyNames = string.Join(", ",
            domainAssemblies.Select(a => a.GetName().Name));

        return $$"""
            <!DOCTYPE html>
            <html lang="sv">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>VGR.Demo.Domain</title>
                <link rel="icon" href="/favicon.svg" type="image/svg+xml">
                <style>
                    * { margin: 0; padding: 0; box-sizing: border-box; }

                    body {
                        font-family: system-ui, -apple-system, sans-serif;
                        background: #0a0a0a;
                        color: #e0e0e0;
                        min-height: 100vh;
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        justify-content: center;
                    }

                    main {
                        text-align: center;
                        padding: 2rem;
                        flex: 1;
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        justify-content: center;
                        gap: 2rem;
                    }

                    h1 {
                        font-size: 2rem;
                        font-weight: 300;
                        letter-spacing: 0.05em;
                        color: #ffffff;
                    }

                    .subtitle {
                        font-size: 0.95rem;
                        color: #888;
                        max-width: 32rem;
                        line-height: 1.6;
                    }

                    .assemblies {
                        font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                        font-size: 0.8rem;
                        color: #0bd6ea;
                        opacity: 0.7;
                    }

                    nav {
                        display: flex;
                        gap: 1.5rem;
                    }

                    nav a {
                        color: #0bd6ea;
                        text-decoration: none;
                        font-size: 0.95rem;
                        padding: 0.5rem 1.25rem;
                        border: 1px solid #1a3a3e;
                        border-radius: 0.25rem;
                        transition: border-color 0.2s, background 0.2s;
                    }

                    nav a:hover {
                        border-color: #0bd6ea;
                        background: rgba(11, 214, 234, 0.05);
                    }

                    footer {
                        padding: 2rem;
                        opacity: 0.4;
                        transition: opacity 0.3s;
                    }

                    footer:hover { opacity: 0.8; }

                    footer img { width: 48px; height: 48px; }
                </style>
            </head>
            <body>
                <main>
                    <h1>VGR.Demo.Domain</h1>
                    <p class="subtitle">
                        Referensarkitektur — E-Clean &amp; Semantic Architecture
                    </p>
                    <p class="assemblies">{{assemblyNames}}</p>
                    <nav>
                        <a href="/domain">Domän</a>
                        <a href="/api">API</a>
                    </nav>
                </main>
                <footer>
                    <a href="https://github.com/bemafred/sky-omega">
                        <img src="/edgar-badge.svg" alt="Sky Omega">
                    </a>
                </footer>
            </body>
            </html>
            """;
    }
}
