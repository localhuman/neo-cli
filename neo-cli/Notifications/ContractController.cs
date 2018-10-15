using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Neo.Ledger;

namespace Neo.Notifications
{


    #region snippet_ControllerSignature
    [Route("v1/notifications/contract")]
    public class ContractsController : ControllerBase
    #endregion
    {
        private NotificationResult defaultResult = new NotificationResult { current_height = Blockchain.Singleton.Height + 1, message = "Invalid Script Hash", results = new List<JToken>()};


        #region snippet_GetByScriptHash
        [HttpGet("{scripthash}")]
        [ProducesResponseType(typeof(NotificationResult), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetByScriptHash(string scripthash, NotificationQuery pageQuery)
        {
            NotificationResult result = defaultResult;

            if( UInt160.TryParse(scripthash, out UInt160 contract))
            {
                result = NotificationDB.Instance.NotificationsForContract(contract, pageQuery);
            }

            result.Paginate(pageQuery);

            return Ok(result);
        }
        #endregion

    }
}