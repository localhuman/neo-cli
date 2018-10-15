using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Neo.Ledger;
using Newtonsoft.Json.Linq;
using Neo.Wallets;

namespace Neo.Notifications
{



    #region snippet_ControllerSignature
    [Route("v1/transaction")]
    public class TransactionController : ControllerBase
    #endregion
    {

        private NotificationResult defaultResult = new NotificationResult { current_height = Blockchain.Singleton.Height + 1, message = "Invalid TX Hash", results = new List<JToken>() };


        #region snippet_GetByHash
        [HttpGet("{hash}")]
        [ProducesResponseType(typeof(NotificationResult), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetByHash(string hash, NotificationQuery pageQuery)
        {
            NotificationResult result = defaultResult;
            if (UInt256.TryParse(hash, out UInt256 tx_hash))
            {
                result = NotificationDB.Instance.NotificationsForTransaction(tx_hash, pageQuery);
            }
            result.Paginate(pageQuery);

            return Ok(result);
        }
        #endregion
    }
}