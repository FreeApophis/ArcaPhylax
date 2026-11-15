// Renders catalog.json (written by tools/ArcaPhylax.PluginPack during the Pages build) as plugin cards.
// Text is set with textContent only: the catalog is data, never markup.
(function () {
  "use strict";

  const platformNames = {
    "win-x64": "Windows x64",
    "win-arm64": "Windows ARM64",
    "linux-x64": "Linux x64",
    "osx-arm64": "macOS (Apple silicon)",
  };

  const container = document.getElementById("plugins");

  function element(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
  }

  function compareVersions(a, b) {
    const pa = a.split(".").map(Number);
    const pb = b.split(".").map(Number);
    for (let i = 0; i < 3; i++) {
      if (pa[i] !== pb[i]) return pa[i] - pb[i];
    }
    return 0;
  }

  function megabytes(bytes) {
    return (bytes / 1024 / 1024).toFixed(1) + " MB";
  }

  function card(plugin) {
    const newest = plugin.packages.map(p => p.version).sort(compareVersions).pop();
    const packages = plugin.packages
      .filter(p => p.version === newest)
      .sort((a, b) => a.platform.localeCompare(b.platform));

    const article = element("article", "card plugin");
    const header = element("header");
    header.append(element("h3", null, plugin.name), element("span", "version", newest));
    article.append(header);
    article.append(element("p", null, plugin.description));
    article.append(element("p", "small muted", "by " + plugin.author + " · SDK " + packages[0].sdkVersion));

    const platforms = element("div", "platforms");
    for (const pkg of packages) {
      const link = element("a", "platform");
      link.href = pkg.url;
      link.download = "";
      link.title = "SHA-256 " + pkg.sha256;
      link.append(element("span", null, platformNames[pkg.platform] || pkg.platform), element("span", "size", megabytes(pkg.size)));
      platforms.append(link);
    }
    article.append(platforms);
    return article;
  }

  fetch("catalog.json", { cache: "no-cache" })
    .then(response => {
      if (!response.ok) throw new Error(response.status + " " + response.statusText);
      return response.json();
    })
    .then(catalog => {
      container.replaceChildren();
      if (!catalog.plugins || catalog.plugins.length === 0) {
        container.append(element("p", "muted", "No plugins have been published yet."));
        return;
      }
      for (const plugin of catalog.plugins) container.append(card(plugin));
    })
    .catch(error => {
      container.replaceChildren(element("p", "muted", "The catalog could not be loaded (" + error.message + ")."));
    });
})();
