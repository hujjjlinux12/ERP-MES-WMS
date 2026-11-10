using Microsoft.EntityFrameworkCore;

public class ErpDbContext : DbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options)
    {
        
    }
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Material>()
            .HasKey(material => material.Code);
        modelBuilder.Entity<Material>()
            .Property(material => material.Code)
            .IsRequired();

        modelBuilder.Entity<Supplier>()
            .HasKey(supplier => supplier.Code);
            
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(order => order.SupplierCode)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne<Material>()
            .WithMany()
            .HasForeignKey(line => line.MaterialCode)
            .OnDelete(DeleteBehavior.Restrict); // 禁止级联删除   
    }
}