// written by malan
using System.Security.Claims;
using Altrium_Project_Backend.Data;
using Altrium_Project_Backend.Repositories.Interfaces;

namespace Altrium_Project_Backend.Security
{
    // Who a manager may hand work to.
    //
    // Work is assigned downwards: a manager can give a record to a rep or to
    // another manager, but not to a leadership account. Leadership is
    // unrestricted. Kept in one place so companies, leads and deals cannot drift
    // apart on a rule that reads the same in all three.
    public static class Assignment
    {
        public static async Task<string?> ValidateAsync(
            ClaimsPrincipal caller,
            IUserRepository users,
            int userId,
            string noun)
        {
            // Leadership may assign to anyone, and anyone may keep their own work.
            if (caller.IsLeadership() || userId == caller.CallerId()) return null;

            var target = await users.GetByIdAsync(userId);
            if (target is null) return "That team member does not exist.";

            if (string.Equals(target.UserRole, Roles.Leadership, StringComparison.OrdinalIgnoreCase))
                return $"{noun} cannot be assigned to a leadership account.";

            return null;
        }
    }
}
