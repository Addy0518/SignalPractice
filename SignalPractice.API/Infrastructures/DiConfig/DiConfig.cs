using SignalPractice.Common.Helpers;

namespace SignalPractice.API.Infrastructures.DiConfig;

public static class DiConfig
{
    //管理註冊
    public static void AddDiConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TokenHelper>();

        services.AddSingleton<DBConnecting>();

        services.AddScoped<PasswordSecureHelper>();

        services.AddScoped<VerifyCodeHelper>();

        //將Service結尾且生命週期相同的物件, 統一註冊
        services.Scan(scan =>
            scan.FromAssemblyOf<Program>() // 1.遍歷Program類別所在程序集中的所有類別
                .AddClasses(classes => // 2.要自動註冊的類別,條件為Service結尾的類別
                    classes.Where(t => t.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
                )
                .AsImplementedInterfaces() // 3.註冊的類別有實作界面
                .WithScopedLifetime() // 4.生命週期設定為Scoped
        );

        // 將Repository結尾且生命週期相同的物件,統一註冊
        services.Scan(scan =>
            scan.FromAssemblyOf<Program>() // 1.遍歷Program類別所在程序集中的所有類別
                .AddClasses(classes => // 2.要自動註冊的類別,條件為Repository結尾的類別
                    classes.Where(t => t.Name.EndsWith("Repository", StringComparison.OrdinalIgnoreCase))
                )
                .AsImplementedInterfaces() // 3.註冊的類別有實作界面
                .WithScopedLifetime() // 4.生命週期設定為Scoped
        );
    }
}
