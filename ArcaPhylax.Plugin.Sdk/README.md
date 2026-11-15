# ArcaPhylax.Plugin.Sdk

Everything needed to write a plugin for [ArcaPhylax](https://freeapophis.github.io/ArcaPhylax/).

The fastest start is the project template:

```
dotnet new install ArcaPhylax.Plugin.Templates
dotnet new arcaphylax-plugin -n Contoso.Hello
```

The package brings:

- `IPlugin` and the shared contracts (`ITabContentViewModel`, `IConfigurationSection`, repositories),
- the Avalonia, Autofac and CommunityToolkit.Mvvm versions the ArcaPhylax host provides,
- build rules that keep a plugin's publish output to its **own** files (the host provides the rest) and set
  the default platforms (`win-x64;win-arm64;linux-x64;osx-arm64`).

Describe your plugin in the project file (`PluginId`, `Title`, `Description`, `Authors`, `Version`), then pack
and sign it with the `ArcaPhylax.PluginPack` tool. See the
[documentation](https://freeapophis.github.io/ArcaPhylax/docs/) for writing, signing and publishing plugins.
