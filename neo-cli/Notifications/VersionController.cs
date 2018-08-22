using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Neo.Core;
using Newtonsoft.Json.Linq;
using Neo.Wallets;

namespace Neo.Notifications
{

    public class VersionResult
    {
        public string version { get; set; }
        public uint current_height { get; set; }
    }


    #region snippet_ControllerSignature
    [Route("v1/version")]
    public class VersionController : ControllerBase
    #endregion
    {

        private VersionResult defaultResult = new VersionResult { version = NotificationDB.Instance.version, current_height = Blockchain.Default.Height + 1 };


        #region snippet_GetVersion
        [HttpGet]
        [ProducesResponseType(typeof(VersionResult), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetVersion()
        {
            return Ok(defaultResult);
        }
        #endregion

    }
}