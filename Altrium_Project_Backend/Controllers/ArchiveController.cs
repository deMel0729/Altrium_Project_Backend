// written by malan
using Altrium_Project_Backend.Data;
using Altrium_Project_Backend.Models;
using Altrium_Project_Backend.Repositories.Interfaces;
using Altrium_Project_Backend.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altrium_Project_Backend.Controllers
{
    // "Recently deleted": the recovery screen for soft-deleted records.
    //
    // Leadership only. A rep deleting their own record is normal; deciding that a
    // deletion was a mistake and reversing it is an administrative act, and the
    // list deliberately spans every rep's records, so it is not scoped by owner.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Leadership)]
    public class ArchiveController : ControllerBase
    {
        private readonly IArchiveRepository _archive;
        private readonly ILogger<ArchiveController> _log;

        public ArchiveController(IArchiveRepository archive, ILogger<ArchiveController> log)
        {
            _archive = archive;
            _log = log;
        }

        // GET /api/Archive
        [HttpGet]
        public async Task<ActionResult<List<ArchivedItem>>> GetAll() => Ok(await _archive.GetAllAsync());

        // GET /api/Archive/entities - the labels the client uses to build its filter.
        [HttpGet("entities")]
        public ActionResult<object> Entities() =>
            Ok(ArchivableEntities.All.Select(e => new { e.Key, e.Label }));

        // POST /api/Archive/deals/7/restore
        [HttpPost("{entity}/{id:int}/restore")]
        public async Task<IActionResult> Restore(string entity, int id)
        {
            var target = ArchivableEntities.Find(entity);
            if (target is null)
                return BadRequest($"Unknown record type '{entity}'.");

            if (!await _archive.RestoreAsync(target, id))
                return NotFound("That record is not in the archive - it may already have been restored.");

            _log.LogWarning("{Label} {Id} restored by user {ActorId}", target.Label, id, User.CallerId());
            return NoContent();
        }
    }
}
