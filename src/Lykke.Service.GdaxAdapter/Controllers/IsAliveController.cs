﻿using System.Net;
using Lykke.Common.Api.Contract.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.GdaxAdapter.Controllers
{
    // NOTE: See https://lykkex.atlassian.net/wiki/spaces/LKEWALLET/pages/35755585/Add+your+app+to+Monitoring
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        /// <summary>
        /// Checks service is alive
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [SwaggerOperation("IsAlive")]
        [ProducesResponseType(typeof(IsAliveResponse), (int)HttpStatusCode.OK)]        
        public IActionResult Get()
        {
            // NOTE: Feel free to extend IsAliveResponse, to display job-specific indicators
            return Ok(new IsAliveResponse
            {
                Name = PlatformServices.Default.Application.ApplicationName,
                Version = PlatformServices.Default.Application.ApplicationVersion,
                Env = Program.EnvInfo,
#if DEBUG
                IsDebug = true,
#else
                IsDebug = false,
#endif
            });
        }
    }
}
