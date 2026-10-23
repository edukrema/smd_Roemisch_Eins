namespace Game.Api.Data;

using Game.Domain;
using Microsoft.EntityFrameworkCore;

public static class UserProfileQueries
{
    /// <summary>Loads the profile or creates it on first call (lazy provisioning).</summary>
    public static async Task<UserProfile> GetOrCreateProfileAsync(this GameDbContext db, Guid userId)
    {
        var profile = await db.UserProfiles.FindAsync(userId);
        if (profile is not null)
        {
            return profile;
        }

        profile = new UserProfile(userId);
        db.UserProfiles.Add(profile);

        try
        {
            await db.SaveChangesAsync();
            return profile;
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            // A concurrent first call was faster. Its profile wins.
            db.Entry(profile).State = EntityState.Detached;
            return await db.UserProfiles.SingleAsync(u => u.Id == userId);
        }
    }
}
