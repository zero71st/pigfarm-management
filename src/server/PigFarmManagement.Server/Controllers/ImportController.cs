using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Server.Services;

namespace PigFarmManagement.Server.Controllers
{
    [ApiController]
    [Route("import")]
    public class ImportController : ControllerBase
    {
        private readonly IPosposImporter _importer;
        private readonly IPosposClient _posposClient;

        public ImportController(IPosposImporter importer, IPosposClient posposClient)
        {
            _importer = importer;
            _posposClient = posposClient;
        }

        /// <summary>
        /// Trigger a one-shot import from POSPOS. By default does not persist mapping unless ?persist=true
        /// Returns the import summary JSON.
        /// </summary>
        [HttpPost("customers")]
        public async Task<IActionResult> ImportCustomers([FromQuery] bool persist = false)
        {
            var summary = await _importer.RunImportAsync(persist);
            return Ok(summary);
        }

        /// <summary>
        /// List POSPOS members available for import (candidates).
        /// </summary>
        [HttpGet("customers/candidates")]
        public async Task<IActionResult> GetCandidates()
        {
            var members = await _posposClient.GetMembersAsync();
            return Ok(members);
        }

        /// <summary>
        /// Import a specific list of POSPOS member ids.
        /// </summary>
        [HttpPost("customers/selected")]
        public async Task<IActionResult> ImportSelected([FromBody] IEnumerable<string> ids, [FromQuery] bool persist = false)
        {
            if (ids == null)
                return BadRequest(new { message = "ids required" });

            var summary = await _importer.RunImportSelectedAsync(ids, persist);
            return Ok(summary);
        }

        /// <summary>
        /// Get the last import summary produced by the in-memory importer.
        /// </summary>
        [HttpGet("customers/summary")]
        public IActionResult GetSummary()
        {
            var summary = _importer.LastSummary;
            if (summary == null)
                return NotFound(new { message = "No import has been run yet." });
            return Ok(summary);
        }
    }
}
