using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApiBlya.Services; 
public class UserActionHistoryConfiguration : IEntityTypeConfiguration<UserActionHistory>
{
    public void Configure(EntityTypeBuilder<UserActionHistory>builder)
    {
        


        builder.HasOne(x=>x.user).WithMany(x=>x.histories).HasForeignKey(x=>x.users_id);
        builder.HasOne(x=>x.UserAction).WithMany(x=>x.users).HasForeignKey(x=>x.actions_id); 
            
    }
}

