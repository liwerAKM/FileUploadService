using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 1. 跨域策略：从配置文件读取允许的地址（核心修改部分）
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificServer", policy =>
        {
            // 从配置文件读取跨域允许的地址（CorsSettings:AllowedOrigins节点）
            var allowedOrigins = builder.Configuration
                .GetSection("CorsSettings:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            // 打印配置信息（方便验证）
            if (allowedOrigins.Length == 0)
            {
                Console.WriteLine("警告：配置文件中未设置跨域允许的地址（CorsSettings:AllowedOrigins）");
            }
            else
            {
                Console.WriteLine($"跨域策略配置：允许以下地址访问 → {string.Join(", ", allowedOrigins)}");
            }

      

            // 应用配置的跨域规则
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // 2. 文件上传大小限制（保持不变）
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
        Console.WriteLine("文件上传限制：50MB");
    });

    builder.Services.AddControllers();
    var app = builder.Build();

    // 3. 绑定外部可访问端口（保持不变）
    app.Urls.Add("http://0.0.0.0:5000");
    Console.WriteLine("服务绑定端口：http://0.0.0.0:5000");

    // 开发环境配置（保持不变）
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        Console.WriteLine("启用开发环境异常页");
    }
    else
    {
        app.UseHsts();
    }

    // 4. 启用跨域（保持不变）
    app.UseCors("AllowSpecificServer");
    Console.WriteLine("已启用跨域策略：AllowSpecificServer");

    app.UseRouting();
    app.MapControllers();

    // 启动服务
    Console.WriteLine("服务开始启动...");
    app.Run();
}
catch (Exception ex)
{
    // 异常处理（保持不变）
    string errorMsg = $"服务启动失败：{ex.Message}\n详细错误：{ex.StackTrace}";
    Console.WriteLine(errorMsg);

    string logPath = Path.Combine(Directory.GetCurrentDirectory(), "startup_error.log");
    File.WriteAllText(logPath, errorMsg);
    Console.WriteLine($"\n错误日志已保存到：{logPath}");

    Console.WriteLine("\n按任意键退出...");
    Console.ReadKey();
}