using API_WateringDashboard.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_WateringDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReadingsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReadingsController(
            AppDbContext db
        )
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var data = await _db.Readings
                .OrderByDescending(r => r.Timestamp)
                .Take(100)
                .ToListAsync();
            return Ok(data);
        }
    }
}
