using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 配置数据库上下文
builder.Services.AddDbContext<ErpDbContext>(options =>
{
    options.UseSqlite("Data Source=erp.db");
});

// 注册控制器所需要的服务
builder.Services.AddControllers();

// 注册物料业务服务
builder.Services.AddScoped<MaterialService>();

// 注册供应商服务
builder.Services.AddScoped<SupplierService>();

// 注册采购订单服务
builder.Services.AddScoped<PurchaseOrderService>();

// 构建应用程序
var app = builder.Build();

// 配置网页应用程序的中间件
app.UseDefaultFiles();
app.UseStaticFiles();

// 将控制器中的路由映射到接口
app.MapControllers();

// 启动应用程序
app.Run();