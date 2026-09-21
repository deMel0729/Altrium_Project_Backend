// written by malan
using System.Security.Claims;
using Altrium_Project_Backend.Data;

namespace Altrium_Project_Backend.Security
{
    // Every controller reads the caller from the validated token through these helpers,
    // never from the request body. This is what stops a rep from claiming to be someone
    // else simply by posting a different user_id.
    public static class CallerExtensions
    {
        public static int CallerId(this ClaimsPrincipal user)
        {
            var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            return int.TryParse(raw, out var id) ? id : 0;
        }

        public static string CallerRole(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        public static bool SeesEverything(this ClaimsPrincipal user) =>
            Roles.SeesEverything(user.CallerRole());

        // The single value that every scoped repository call takes:
        //   null    -> no row filter (manager / leadership)
        //   an id   -> only rows owned by that user (sales rep)
        public static int? OwnerFilter(this ClaimsPrincipal user) =>
            user.SeesEverything() ? null : user.CallerId();

        // Who should own a record the caller is creating. A rep can only ever create
        // records for themselves; a manager may assign work to a rep.
        public static int OwnerForNewRecord(this ClaimsPrincipal user, int requestedOwnerId)
        {
            if (!user.SeesEverything()) return user.CallerId();
            return requestedOwnerId > 0 ? requestedOwnerId : user.CallerId();
        }
    }
}
