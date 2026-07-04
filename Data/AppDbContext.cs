using Microsoft.EntityFrameworkCore;
using MyApiBlya.Services;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserAction> UserActions {get;set;}
    public DbSet<UserActionHistory> UserActionHistories {get;set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(x=>x.Login).IsUnique();
            entity.HasIndex(x=>x.Id).IsUnique();

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Login).HasColumnName("login");
            entity.Property(x => x.Password).HasColumnName("password");
            entity.Property(x => x.Email).HasColumnName("email");
            entity.Property(x => x.RefreshTokenHash).HasColumnName("refreshToken");
            entity.Property(x => x.Role).HasColumnName("role");
            entity.Property(x => x.Provider).HasColumnName("provider");
            entity.Property(x => x.ProviderUserId).HasColumnName("provideruserid");
            entity.HasIndex(x=>x.ProviderUserId).IsUnique();
            entity.Property(x=>x.BlockedUntill).HasColumnName("BlockedUntill");
            entity.Property(x=>x.Cause).HasColumnName("Cause");
            entity.Property(x => x.CreatedAt).HasColumnName("createdat");
            entity.Property(x => x.IsBlocked).HasColumnName("isblocked").HasDefaultValue(false);
            entity.Property(x => x.RefreshTokenExpiresAt).HasColumnName("refreshTokenExpiresAt");
        });

       

        modelBuilder.ApplyConfiguration(new UserActionHistoryConfiguration());

        modelBuilder.Entity<UserAction>(entity =>
        {
            entity.HasKey(x=>x.Id);
            entity.Property(x=>x.Id).HasColumnName("Id"); 
            entity.Property(x=>x.Action).HasColumnName("action"); 
            entity.HasIndex(x=>x.Action);
        });
        modelBuilder.Entity<UserActionHistory>(entity =>
        {
            entity.HasKey(x=>x.Id);
            entity.Property(x=>x.Id).HasColumnName("id");
            entity.Property(x=>x.users_id).HasColumnName("user");
            entity.Property(x=>x.actions_id).HasColumnName("action");
            entity.Property(x=>x.CreatedAt).HasColumnName("createdat");
        });


    }
}


