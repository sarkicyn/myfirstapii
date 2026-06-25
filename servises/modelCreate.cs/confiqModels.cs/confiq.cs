using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApiBlya.Services;

public class Confiq : IEntityTypeConfiguration<usersPerm>
{
    public void Configure(EntityTypeBuilder<usersPerm> builder)
    {
        builder.ToTable("users_permissions");

        builder.HasKey(up => new { up.users_id, up.perms_id });

      

        builder
            .HasOne(up => up.user)
            .WithMany(u => u.perms)
            .HasForeignKey(up => up.users_id)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(up => up.permissions)
            .WithMany(p => p.users)
            .HasForeignKey(up => up.perms_id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
