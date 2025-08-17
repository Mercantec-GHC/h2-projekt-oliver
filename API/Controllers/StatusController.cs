using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly AppDBContext _db;

        public StatusController(AppDBContext db)
        {
            _db = db;
        }

        /// Tjekker om API kører
        [HttpGet("healthcheck")]
        public IActionResult HealthCheck()
        {
            return Ok(new { status = "OK", message = "API'en er kørende!" });
        }

        /// Tjekker om databasen er tilgængelig (EF Core)
        [HttpGet("dbhealthcheck")]
        public async Task<IActionResult> DBHealthCheck()
        {
            try
            {
                var canConnect = await _db.Database.CanConnectAsync();
                if (canConnect)
                    return Ok(new { status = "OK", message = "Database er kørende!" });

                return StatusCode(500, new { status = "Error", message = "Kan ikke forbinde til databasen." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "Error", message = "Fejl ved forbindelse til database: " + ex.Message });
            }
        }

        /// Simpelt ping-endpoint til test af API
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { status = "OK", message = "Pong 🏓" });
        }
    }
}
