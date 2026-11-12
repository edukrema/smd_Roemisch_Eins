namespace Game.Api.Data;

using Game.Api.Auth;
using Game.Domain;
using Microsoft.EntityFrameworkCore;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(account =>
        {
            account.Property(a => a.Id).ValueGeneratedNever();
            account.Property(a => a.Username).HasMaxLength(UserAccount.UsernameMaxLength);
            account.Property(a => a.PasswordHash).HasMaxLength(200);
            account.HasIndex(a => a.Username).IsUnique();
        });

        modelBuilder.Entity<UserProfile>(user =>
        {
            user.Property(u => u.Id).ValueGeneratedNever();
            user.Property(u => u.Username).HasMaxLength(UserProfile.UsernameMaxLength);
            user.Property(u => u.UsernameNormalized).HasMaxLength(UserProfile.UsernameMaxLength);
            // REQ06: unique, case-insensitive
            user.HasIndex(u => u.UsernameNormalized).IsUnique();
            user.Ignore(u => u.HasUsername);
        });

        modelBuilder.Entity<Game>(game =>
        {
            game.Property(g => g.Id).ValueGeneratedNever();
            game.Property(g => g.Status).HasConversion<string>().HasMaxLength(20);
            game.Ignore(g => g.IsFull);
            game.HasIndex(g => g.Status);
            game.HasOne<UserProfile>().WithMany().HasForeignKey(g => g.GameMasterId).OnDelete(DeleteBehavior.Restrict);
            game.HasMany(g => g.Participants).WithOne().HasForeignKey(p => p.GameId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Participant>(participant =>
        {
            participant.ToTable("Participants");
            participant.Property(p => p.Id).ValueGeneratedNever();
            participant.HasIndex(p => new { p.GameId, p.UserId }).IsUnique();
            participant.HasOne<UserProfile>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
            participant.HasMany(p => p.Attributes).WithOne().HasForeignKey(a => a.ParticipantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParticipantAttribute>(attribute =>
        {
            attribute.ToTable("ParticipantAttributes");
            attribute.Property(a => a.Id).ValueGeneratedNever();
            attribute.Property(a => a.Key).HasMaxLength(ParticipantAttribute.KeyMaxLength);
            attribute.Property(a => a.Value).HasMaxLength(ParticipantAttribute.ValueMaxLength);
            attribute.HasIndex(a => new { a.ParticipantId, a.Key }).IsUnique();
        });
    }
}
