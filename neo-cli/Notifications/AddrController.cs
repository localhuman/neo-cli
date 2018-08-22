using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Neo.Core;
using Newtonsoft.Json.Linq;
using Neo.Wallets;

namespace Neo.Notifications
{



    #region snippet_ControllerSignature
    [Route("v1/notifications/addr")]
    public class AddrController : ControllerBase
    #endregion
    {

        private NotificationResult defaultResult = new NotificationResult { current_height = Blockchain.Default.Height + 1, message = "Invalid Address", results = new List<JToken>() };


        #region snippet_GetByAddr
        [HttpGet("{addr}")]
        [ProducesResponseType(typeof(NotificationResult), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetByAddr(string addr, NotificationQuery pageQuery)
        {
            NotificationResult result = defaultResult;

            if( addr.Length == 34)
            {
                result = NotificationDB.Instance.NotificationsForAddress(Wallet.ToScriptHash(addr), pageQuery);

            } else if( UInt160.TryParse(addr, out UInt160 address))
            {
                result = NotificationDB.Instance.NotificationsForAddress(address, pageQuery);
            }

            result.Paginate(pageQuery);

            return Ok(result);
        }
        #endregion

    }
}