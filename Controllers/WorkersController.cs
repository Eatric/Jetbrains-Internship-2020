using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIServerBlazor.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CIServerBlazor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkersController : ControllerBase
    {
        // GET: api/Workers
        [HttpGet("{url}/{branch}/{build}")]
        public string Get(string url, string branch, string build)
        {
	        BuildService.GetService().AddWorker(url, branch, build);
	        return "Successful";
        }

        // POST: api/Workers
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public ActionResult Post([FromBody] List<WorkerDto> value)
        {
	        foreach (var val in value)
	        {
		        BuildService.GetService().AddWorker(val.Url, val.Branch, val.Build);
	        }

	        return Ok();
        }
    }
}
