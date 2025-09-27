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

        public ImportController(IPosposImporter importer)
        {
            _importer = importer;
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
