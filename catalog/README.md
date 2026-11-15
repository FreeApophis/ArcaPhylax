# Third-party plugin listings

Each `*.json` file here lists one publisher's plugins in the ArcaPhylax plugin catalog. The files use the
`catalog.json` format, with **absolute `https://` links** to packages the publisher hosts (for example on
their GitHub Releases).

On every pull request, `.github/workflows/check-listings.yml` downloads every listed package and checks that:

- it has exactly the listed size and SHA-256,
- its signature is valid,
- its `plugin.json` matches the listing (id, version, platform, SDK version), and
- it is signed by an **endorsed publisher** (see `site/publishers.json`) that owns the plugin id.

After merging, the website workflow runs the same check again and adds the listings to the published catalog.

## Listing your plugin

1. **Get endorsed.** Open a *Publisher endorsement* issue with your public key (from
   `dotnet run --project tools/ArcaPhylax.PluginPack -- keygen my.key`) and the plugin id prefix you want
   (for example `Contoso`, which covers `Contoso` and `Contoso.*`).
2. **Pack and upload.** For each platform, pack the plugin and upload the `.arcaplugin` files to a release:

   ```
   dotnet run --project tools/ArcaPhylax.PluginPack -- pack path/to/Hello.csproj --key my.key --rid win-x64 \
       --out out --catalog catalog/contoso.json \
       --base-url https://github.com/contoso/hello/releases/download/v1.0.0
   ```

   `--catalog` with `--base-url` writes (or updates) your listing file with the release download URLs.
3. **Open a pull request** that adds or updates `catalog/<you>.json`. To release a new version, pack again
   with the new release URL; older versions can stay listed.

Packages must stay downloadable at the listed URL with the listed hash. Never replace a released file:
publish a new version instead.
