using Microsoft.EntityFrameworkCore;
using MyApiBlya.Services;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> users { get; set; }
    public DbSet<Permissions> permissions { get; set; }
    public DbSet<usersPerm> usersPermissions { get; set; }
    public DbSet<History> histories {get;set;}
    public DbSet<UsersHistory> UsersHistory {get;set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Confiq());

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
            entity.Property(x => x.CreatedAt).HasColumnName("createdat");
            entity.Property(x => x.IsBlocked).HasColumnName("isblocked").HasDefaultValue(false);
            entity.Property(x => x.RefreshTokenExpiresAt).HasColumnName("refreshTokenExpiresAt");
        });

        modelBuilder.Entity<Permissions>(entity =>
        {
            

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Permission).HasColumnName("permission");
        });
        modelBuilder.Entity<usersPerm>(entity =>
{
    

    entity.Property(x => x.users_id)
        .HasColumnName("users_id");

    entity.Property(x => x.perms_id)
        .HasColumnName("perms_id");
});
        modelBuilder.ApplyConfiguration(new ConfHistory());

        modelBuilder.Entity<History>(entity =>
        {
            entity.HasKey(x=>x.Id);
            entity.Property(x=>x.Id).HasColumnName("Id"); 
            entity.Property(x=>x.Action).HasColumnName("action"); 
        });
        modelBuilder.Entity<UsersHistory>(entity =>
        {
            entity.HasKey(x=>x.Id);
            entity.Property(x=>x.Id).HasColumnName("id");
            entity.Property(x=>x.users_id).HasColumnName("user");
            entity.Property(x=>x.actions_id).HasColumnName("action");
            entity.Property(x=>x.CreatedAt).HasColumnName("createdat");
        });


    }
}
