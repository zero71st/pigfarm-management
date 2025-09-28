using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Server.Services;

namespace PigFarmManagement.Server.Controllers
{
    [ApiController]
    [Route("import/feeds")]
    public class ImportFeedsController : ControllerBase
    {
        private readonly IPosposInvoiceImporter _importer;

        public ImportFeedsController(IPosposInvoiceImporter importer)
        {
            _importer = importer;
        }

        /// <summary>
        /// Import POSPOS transactions for a date range (inclusive). Dates should be yyyy-MM-dd.
        /// </summary>
        [HttpPost("date-range")]
        public async Task<IActionResult> ImportByDate([FromQuery] string from, [FromQuery] string to)
        {
            if (!DateTime.TryParse(from, out var fromDt) || !DateTime.TryParse(to, out var toDt))
                return BadRequest(new { message = "Invalid from/to date parameters. Use yyyy-MM-dd or ISO date strings." });

            var result = await _importer.ImportByDateRangeAsync(fromDt, toDt);
            return Ok(result);
        }

        /// <summary>
        /// Import POSPOS transactions for a specific customer code.
        /// </summary>
        [HttpPost("customer/{code}")]
        public async Task<IActionResult> ImportByCustomer([FromRoute] string code, [FromQuery] string? from = null, [FromQuery] string? to = null)
        {
            DateTime? fromDt = null, toDt = null;
            if (!string.IsNullOrWhiteSpace(from) && !DateTime.TryParse(from, out var f)) return BadRequest(new { message = "Invalid from date" });
            if (!string.IsNullOrWhiteSpace(to) && !DateTime.TryParse(to, out var t)) return BadRequest(new { message = "Invalid to date" });
            if (!string.IsNullOrWhiteSpace(from)) fromDt = DateTime.Parse(from);
            if (!string.IsNullOrWhiteSpace(to)) toDt = DateTime.Parse(to);

            var result = await _importer.ImportByCustomerCodeAsync(code, fromDt, toDt);
            return Ok(result);
        }
    }
}
