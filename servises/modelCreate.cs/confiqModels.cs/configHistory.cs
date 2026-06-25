using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApiBlya.Services; 
public class ConfHistory : IEntityTypeConfiguration<UsersHistory>
{
    public void Configure(EntityTypeBuilder<UsersHistory>builder)
    {
        


        builder.HasOne(x=>x.user).WithMany(x=>x.histories).HasForeignKey(x=>x.users_id);
        builder.HasOne(x=>x.history).WithMany(x=>x.users).HasForeignKey(x=>x.actions_id); 
            
    }
}
