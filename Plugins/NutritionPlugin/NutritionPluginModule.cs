using ArcaPhylax.Abstractions;
using Autofac;
using NutritionPlugin.Data;
using NutritionPlugin.ViewModels;
using NutritionPlugin.Views;

namespace NutritionPlugin;

internal sealed class NutritionPluginModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Persistence: the plugin owns its own SQLite file (nutrition.db). It cannot reach the host's
        // ISqliteConnectionFactory — that lives in ArcaPhylax.Infrastructure, which plugins never
        // reference — so it carries a self-contained factory, mirroring CharacterArchivePlugin.
        builder.RegisterType<NutritionConnectionFactory>().As<INutritionConnectionFactory>().SingleInstance();
        builder.RegisterType<SqliteNutritionRepository>().As<INutritionRepository>().SingleInstance();

        // Tab.
        builder.RegisterType<NutritionViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<NutritionView>();

        base.Load(builder);
    }
}
